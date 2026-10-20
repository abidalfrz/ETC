using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Recruitments;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Features.Applications;
public interface IApplicationService
{
    Task<int> SubmitAsync(ApplicationInput input,CancellationToken ct=default);
    Task CancelAsync(int applicationId,CancellationToken ct=default);
    Task<Page<MyApplicationRow>> ListMineAsync(int page,CancellationToken ct=default);
    Task<Page<ApplicantRow>> ListApplicantsAsync(int recruitmentId,int page,CancellationToken ct=default);
    Task<Page<ApplicantRow>> ListPositionApplicantsAsync(int recruitmentId,int positionId,int page,CancellationToken ct=default);
    Task DecideAsync(int applicationId,ApplicationStatus decision,CancellationToken ct=default);
    Task<bool> HasAppliedAsync(int recruitmentId,CancellationToken ct=default);
}
public sealed class ApplicationService(SqlConnectionFactory connections,SessionState session,TimeProvider clock):IApplicationService
{
    public Task<Page<ApplicantRow>> ListPositionApplicantsAsync(int recruitmentId,int positionId,int page,CancellationToken ct=default)=>ListApplicantsCoreAsync(recruitmentId,positionId,page,ct);
    public async Task<bool> HasAppliedAsync(int recruitmentId,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await using var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Applications WHERE StudentId=@actor AND RecruitmentId=@id",("actor",actor),("id",recruitmentId));return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
    }
    private sealed record Header(int StudentId,int RecruitmentId,int PositionId,ApplicationStatus Status);
    private sealed record RecruitmentHeader(int OwnerId,RecruitmentStatus Status,DateTimeOffset? DeadlineUtc);
    private sealed record ApplicantData(int Id,int StudentId,int PositionId,string PositionName,string Message,string? PortfolioUrl,ApplicationStatus Status,DateTimeOffset SubmittedAtUtc,DateTimeOffset? DecidedAtUtc);
    private static async Task<Header> ReadAsync(SqlConnection c,SqlTransaction? tx,int id,CancellationToken ct)
    {
        await using var cmd=Db.Command(c,tx,"SELECT StudentId,RecruitmentId,PositionId,Status FROM Applications WHERE Id=@id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER",("id",id));
        return await Db.JsonAsync<Header>(cmd,ct)??throw new DomainException("NotFound","Lamaran tidak ditemukan.");
    }
    public async Task<int> SubmitAsync(ApplicationInput input,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();var message=Guard.Text(input.Message,"Pesan perkenalan",2000);Guard.Url(input.PortfolioUrl);
        if(!await new ProfileService(connections,session,clock).IsMineCompleteAsync(ct))throw new DomainException("Validation","Lengkapi nama, angkatan, dan skill sebelum melamar.");
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await RecruitmentService.LockAsync(c,tx,input.RecruitmentId,ct);
        RecruitmentHeader header;
        await using(var cmd=Db.Command(c,tx,"SELECT OwnerId,Status,DeadlineUtc FROM Recruitments WHERE Id=@id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER",("id",input.RecruitmentId)))header=(await Db.JsonAsync<RecruitmentHeader>(cmd,ct))!;
        if(header.OwnerId==actor)throw new DomainException("Validation","Anda tidak dapat melamar rekrutmen sendiri.");
        await using(var cmd=Db.Command(c,tx,"SELECT COUNT(*) FROM Applications WHERE StudentId=@actor AND RecruitmentId=@id",("actor",actor),("id",input.RecruitmentId)))
            if(Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0)throw new DomainException("Conflict","Anda sudah pernah melamar rekrutmen ini.");
        var position=(await RecruitmentService.PositionsAsync(c,tx,input.RecruitmentId,ct)).SingleOrDefault(x=>x.Id==input.PositionId)??throw new DomainException("Validation","Posisi tidak sesuai rekrutmen.");
        if(!RecruitmentRules.CanApply(header.Status,header.DeadlineUtc,position.AcceptedCount,position.Capacity,clock.GetUtcNow()))throw new DomainException("Unavailable","Pendaftaran posisi ini sudah tidak tersedia.");
        await using var insert=Db.Command(c,tx,"INSERT Applications(StudentId,RecruitmentId,PositionId,Message,PortfolioUrl,SubmittedAtUtc) OUTPUT INSERTED.Id VALUES(@student,@recruitment,@position,@message,@url,@now)",("student",actor),("recruitment",input.RecruitmentId),("position",input.PositionId),("message",message),("url",input.PortfolioUrl),("now",clock.GetUtcNow()));
        try{var id=Convert.ToInt32(await insert.ExecuteScalarAsync(ct));await tx.CommitAsync(ct);return id;}
        catch(SqlException e)when(e.Number is 2601 or 2627){throw new DomainException("Conflict","Anda sudah pernah melamar rekrutmen ini.");}
    }
    public async Task CancelAsync(int applicationId,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        var initial=await ReadAsync(c,tx,applicationId,ct);await RecruitmentService.LockAsync(c,tx,initial.RecruitmentId,ct);var row=await ReadAsync(c,tx,applicationId,ct);
        if(row.StudentId!=actor)throw new DomainException("Unauthorized","Hanya pelamar yang dapat membatalkan lamaran.");
        if(row.Status!=ApplicationStatus.Pending)throw new DomainException("Conflict","Hanya lamaran menunggu yang dapat dibatalkan.");
        await using var cmd=Db.Command(c,tx,"UPDATE Applications SET Status=3,DecidedAtUtc=@now WHERE Id=@id AND Status=0",("id",applicationId),("now",clock.GetUtcNow()));await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task DecideAsync(int applicationId,ApplicationStatus decision,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();if(decision is not(ApplicationStatus.Accepted or ApplicationStatus.Rejected))throw new DomainException("Validation","Keputusan seleksi tidak valid.");
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        var initial=await ReadAsync(c,tx,applicationId,ct);await RecruitmentService.LockAsync(c,tx,initial.RecruitmentId,ct);var row=await ReadAsync(c,tx,applicationId,ct);
        await RequireOwnerAsync(c,tx,row.RecruitmentId,actor,ct);
        if(row.Status!=ApplicationStatus.Pending)throw new DomainException("Conflict","Lamaran ini sudah memiliki hasil akhir.");
        if(decision==ApplicationStatus.Accepted)
        {
            var position=(await RecruitmentService.PositionsAsync(c,tx,row.RecruitmentId,ct)).Single(x=>x.Id==row.PositionId);
            if(!position.HasSpace)throw new DomainException("CapacityFull","Posisi ini sudah penuh. Muat ulang data rekrutmen.");
        }
        await using var cmd=Db.Command(c,tx,"UPDATE Applications SET Status=@status,DecidedAtUtc=@now WHERE Id=@id AND Status=0",("id",applicationId),("status",(int)decision),("now",clock.GetUtcNow()));await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    private static async Task RequireOwnerAsync(SqlConnection c,SqlTransaction? tx,int recruitment,int actor,CancellationToken ct)
    {
        await using var cmd=Db.Command(c,tx,"SELECT OwnerId FROM Recruitments WHERE Id=@id",("id",recruitment));
        if(await cmd.ExecuteScalarAsync(ct) is not int owner||owner!=actor)throw new DomainException("Unauthorized","Hanya pembuat rekrutmen yang dapat meninjau pelamar.");
    }
    public async Task<Page<MyApplicationRow>> ListMineAsync(int page,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);int total;
        await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Applications WHERE StudentId=@actor",("actor",actor)))total=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        page=Math.Clamp(page,1,Math.Max(1,(total+11)/12));
        await using var query=Db.Command(c,null,"SELECT a.Id,a.RecruitmentId,r.Title AS RecruitmentTitle,p.Name AS PositionName,a.SubmittedAtUtc,a.Status FROM Applications a JOIN Recruitments r ON r.Id=a.RecruitmentId JOIN Positions p ON p.Id=a.PositionId WHERE a.StudentId=@actor ORDER BY a.SubmittedAtUtc DESC,a.Id DESC OFFSET @offset ROWS FETCH NEXT 12 ROWS ONLY FOR JSON PATH",("actor",actor),("offset",(page-1)*12));
        return new(await Db.JsonAsync<List<MyApplicationRow>>(query,ct)??[],total,page);
    }
    public Task<Page<ApplicantRow>> ListApplicantsAsync(int recruitmentId,int page,CancellationToken ct=default)=>ListApplicantsCoreAsync(recruitmentId,null,page,ct);
    private async Task<Page<ApplicantRow>> ListApplicantsCoreAsync(int recruitmentId,int? positionId,int page,CancellationToken ct)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await RequireOwnerAsync(c,null,recruitmentId,actor,ct);int total;
        await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Applications WHERE RecruitmentId=@id AND (@position=0 OR PositionId=@position)",("id",recruitmentId),("position",positionId??0)))total=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        page=Math.Clamp(page,1,Math.Max(1,(total+11)/12));
        List<ApplicantData> data;
        await using(var cmd=Db.Command(c,null,"SELECT a.Id,a.StudentId,a.PositionId,p.Name AS PositionName,a.Message,a.PortfolioUrl,a.Status,a.SubmittedAtUtc,a.DecidedAtUtc FROM Applications a JOIN Positions p ON p.Id=a.PositionId WHERE a.RecruitmentId=@id AND (@position=0 OR a.PositionId=@position) ORDER BY a.SubmittedAtUtc DESC,a.Id DESC OFFSET @offset ROWS FETCH NEXT 12 ROWS ONLY FOR JSON PATH",("id",recruitmentId),("position",positionId??0),("offset",(page-1)*12)))data=await Db.JsonAsync<List<ApplicantData>>(cmd,ct)??[];
        var rows=new List<ApplicantRow>();var profiles=new ProfileService(connections,session,clock);
        foreach(var a in data)rows.Add(new(a.Id,await profiles.GetAsync(a.StudentId,ct),a.PositionId,a.PositionName,a.Message,a.PortfolioUrl,a.Status,a.SubmittedAtUtc,a.DecidedAtUtc));
        return new(rows,total,page);
    }
}
