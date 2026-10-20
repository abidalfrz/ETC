using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Profiles;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Features.Recruitments;
public interface IRecruitmentService
{
    Task<int> SaveDraftAsync(int? id,RecruitmentInput input,CancellationToken ct=default);
    Task UpdatePublishedAsync(int id,RecruitmentInput input,CancellationToken ct=default);
    Task PublishAsync(int id,CancellationToken ct=default);
    Task CloseAsync(int id,CancellationToken ct=default);
    Task DeleteDraftAsync(int id,CancellationToken ct=default);
    Task<RecruitmentDetail> GetAsync(int id,CancellationToken ct=default);
    Task<Page<RecruitmentSummary>> ListMineAsync(int page,CancellationToken ct=default);
}
public sealed class RecruitmentService(SqlConnectionFactory connections,SessionState session,TimeProvider clock) : IRecruitmentService
{
    private sealed record Stored(int OwnerId,RecruitmentStatus Status,DateTimeOffset? PublishedAtUtc,DateTimeOffset UpdatedAtUtc,RecruitmentInput Data);
    private const string DataColumns="Title,Category,ActivityName,Description,DeadlineUtc,StartsOn,EndsOn,Mode,Location,FeeKind,FeeAmount,FeeNote,InfoUrl,ContactName,ContactChannel";
    internal static async Task LockAsync(SqlConnection c,SqlTransaction tx,int id,CancellationToken ct)
    {
        await using var cmd=Db.Command(c,tx,"SELECT Id FROM Recruitments WITH(UPDLOCK,HOLDLOCK) WHERE Id=@id",("id",id));
        if(await cmd.ExecuteScalarAsync(ct) is null)throw new DomainException("NotFound","Rekrutmen tidak ditemukan.");
    }
    private static void Owner(Stored row,int actor)
    {
        if(row.OwnerId!=actor)throw new DomainException("Unauthorized","Hanya pembuat yang dapat mengelola rekrutmen ini.");
    }
    private static void Validate(RecruitmentInput x,bool publish,DateTimeOffset now)
    {
        Guard.Text(x.Title,"Judul",200,publish);Guard.Text(x.ActivityName,"Nama kegiatan",200,publish);Guard.Text(x.Description,"Deskripsi",10000,publish);
        Guard.Text(x.Location,"Lokasi",500,false);Guard.Text(x.ContactName,"Nama kontak",120,publish);Guard.Text(x.ContactChannel,"Kanal kontak",500,publish);Guard.Text(x.FeeNote,"Keterangan biaya",2000,false);Guard.Url(x.InfoUrl);
        if(!Enum.IsDefined(x.Category)||!Enum.IsDefined(x.FeeKind)||(x.Mode.HasValue&&!Enum.IsDefined(x.Mode.Value)))throw new DomainException("Validation","Kategori atau mode tidak valid.");
        if(x.StartsOn.HasValue&&x.EndsOn<x.StartsOn)throw new DomainException("Validation","Tanggal selesai tidak boleh mendahului mulai.");
        if(x.FeeAmount<0||x.FeeAmount>9999999999999999.99m||(x.FeeKind==FeeKind.Paid&&(x.FeeAmount is null or <=0||string.IsNullOrWhiteSpace(x.FeeNote))))throw new DomainException("Validation","Biaya berbayar harus positif dan memiliki keterangan.");
        if(publish&&(x.DeadlineUtc is null||x.DeadlineUtc<=now||x.Mode is null||x.Positions.Count==0||(x.Mode!=ActivityMode.Online&&string.IsNullOrWhiteSpace(x.Location))))throw new DomainException("Validation","Lengkapi posisi, mode/lokasi, dan tenggat pendaftaran di masa depan.");
        foreach(var p in x.Positions)
        {
            Guard.Text(p.Name,"Nama posisi",120);
            if(p.Capacity<=0||(publish&&p.SkillIds.Count==0))throw new DomainException("Validation","Kuota harus positif dan posisi terpublikasi membutuhkan skill.");
        }
    }
    private static async Task<Stored> ReadAsync(SqlConnection c,SqlTransaction? tx,int id,CancellationToken ct)
    {
        await using var cmd=Db.Command(c,tx,$"SELECT OwnerId,Status,PublishedAtUtc,UpdatedAtUtc,JSON_QUERY((SELECT {DataColumns} FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)) AS Data FROM Recruitments WHERE Id=@id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER",("id",id));
        var row=await Db.JsonAsync<Stored>(cmd,ct)??throw new DomainException("NotFound","Rekrutmen tidak ditemukan.");
        var positions=await PositionsAsync(c,tx,id,ct);
        return row with{Data=row.Data with{Positions=positions.Select(p=>new PositionInput(p.Id,p.Name,p.Capacity,p.Skills.Select(s=>s.Id).ToArray())).ToArray()}};
    }
    internal static async Task<IReadOnlyList<PositionDetail>> PositionsAsync(SqlConnection c,SqlTransaction? tx,int id,CancellationToken ct)
    {
        await using var cmd=Db.Command(c,tx,"SELECT p.Id,p.Name,p.Capacity,(SELECT COUNT(*) FROM Applications a WHERE a.PositionId=p.Id AND a.Status=1) AS AcceptedCount,JSON_QUERY((SELECT s.Id,s.Name FROM PositionSkills ps JOIN Skills s ON s.Id=ps.SkillId WHERE ps.PositionId=p.Id ORDER BY s.Name FOR JSON PATH)) AS Skills FROM Positions p WHERE RecruitmentId=@id ORDER BY p.Id FOR JSON PATH",("id",id));
        return await Db.JsonAsync<List<PositionDetail>>(cmd,ct)??[];
    }
    public Task<int> SaveDraftAsync(int? id,RecruitmentInput input,CancellationToken ct=default)=>SaveAsync(id,input,false,ct);
    public async Task UpdatePublishedAsync(int id,RecruitmentInput input,CancellationToken ct=default)=>await SaveAsync(id,input,true,ct);
    private async Task<int> SaveAsync(int? id,RecruitmentInput x,bool published,CancellationToken ct)
    {
        var actor=session.RequireStudentId();var now=clock.GetUtcNow();
        if(!await new ProfileService(connections,session,clock).IsMineCompleteAsync(ct))throw new DomainException("Validation","Lengkapi nama, angkatan, dan skill terlebih dahulu.");
        Validate(x,false,now);
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        var hasApps=false;
        if(id.HasValue)
        {
            await LockAsync(c,tx,id.Value,ct);var old=await ReadAsync(c,tx,id.Value,ct);Owner(old,actor);
            if((!published&&old.Status!=RecruitmentStatus.Draft)||(published&&old.Status==RecruitmentStatus.Draft))throw new DomainException("Conflict","Gunakan tindakan yang sesuai status rekrutmen.");
            await using(var cmd=Db.Command(c,tx,"SELECT COUNT(*) FROM Applications WHERE RecruitmentId=@id",("id",id)))hasApps=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
            if(hasApps)
            {
                bool same=old.Data.Positions.Count==x.Positions.Count&&old.Data.Positions.All(p=>x.Positions.Any(q=>q.Id==p.Id&&q.Name.Trim()==p.Name&&q.Capacity==p.Capacity&&q.SkillIds.Order().SequenceEqual(p.SkillIds.Order())));
                if(!same)throw new DomainException("Conflict","Posisi, skill, dan kuota terkunci setelah ada lamaran.");
            }
            if(published)
            {
                if(x.DeadlineUtc!=old.Data.DeadlineUtc&&(old.Status!=RecruitmentStatus.Open||old.Data.DeadlineUtc<=now||!(await PositionsAsync(c,tx,id.Value,ct)).Any(p=>p.HasSpace)))throw new DomainException("Conflict","Tenggat hanya dapat diubah saat pendaftaran tersedia.");
                Validate(x,true,x.DeadlineUtc==old.Data.DeadlineUtc?DateTimeOffset.MinValue:now);
            }
        }
        var amount=x.FeeKind==FeeKind.Free?0:x.FeeKind==FeeKind.Unknown?null:x.FeeAmount;
        (string,object?)[] values=[("id",id),("owner",actor),("title",x.Title.Trim()),("category",(int)x.Category),("activity",x.ActivityName.Trim()),("description",x.Description.Trim()),("deadline",x.DeadlineUtc?.ToUniversalTime()),("starts",x.StartsOn),("ends",x.EndsOn),("mode",x.Mode is null?null:(int)x.Mode),("location",x.Location),("feeKind",(int)x.FeeKind),("feeAmount",amount),("feeNote",x.FeeNote),("info",x.InfoUrl),("contactName",x.ContactName),("contact",x.ContactChannel),("now",now)];
        var sql=id is null?"INSERT Recruitments(OwnerId,Title,Category,ActivityName,Description,DeadlineUtc,StartsOn,EndsOn,Mode,Location,FeeKind,FeeAmount,FeeNote,InfoUrl,ContactName,ContactChannel,UpdatedAtUtc) OUTPUT INSERTED.Id VALUES(@owner,@title,@category,@activity,@description,@deadline,@starts,@ends,@mode,@location,@feeKind,@feeAmount,@feeNote,@info,@contactName,@contact,@now)":"UPDATE Recruitments SET Title=@title,Category=@category,ActivityName=@activity,Description=@description,DeadlineUtc=@deadline,StartsOn=@starts,EndsOn=@ends,Mode=@mode,Location=@location,FeeKind=@feeKind,FeeAmount=@feeAmount,FeeNote=@feeNote,InfoUrl=@info,ContactName=@contactName,ContactChannel=@contact,UpdatedAtUtc=@now WHERE Id=@id; SELECT @id;";
        await using(var cmd=Db.Command(c,tx,sql,values))id=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        try
        {
            if(!hasApps)
            {
                await using(var cmd=Db.Command(c,tx,"DELETE PositionSkills WHERE PositionId IN(SELECT Id FROM Positions WHERE RecruitmentId=@id); DELETE Positions WHERE RecruitmentId=@id;",("id",id)))await cmd.ExecuteNonQueryAsync(ct);
                foreach(var p in x.Positions)
                {
                    int positionId;await using(var cmd=Db.Command(c,tx,"INSERT Positions(RecruitmentId,Name,Capacity) OUTPUT INSERTED.Id VALUES(@id,@name,@capacity)",("id",id),("name",p.Name.Trim()),("capacity",p.Capacity)))positionId=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
                    foreach(var skill in p.SkillIds.Distinct())
                    {await using var cmd=Db.Command(c,tx,"INSERT PositionSkills VALUES(@position,@skill)",("position",positionId),("skill",skill));await cmd.ExecuteNonQueryAsync(ct);}
                }
            }
            await tx.CommitAsync(ct);return id.Value;
        }
        catch(SqlException e)when(e.Number==547){throw new DomainException("Validation","Skill yang dipilih tidak tersedia.");}
    }
    public async Task PublishAsync(int id,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();
        if(!await new ProfileService(connections,session,clock).IsMineCompleteAsync(ct))throw new DomainException("Validation","Lengkapi profil terlebih dahulu.");
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await LockAsync(c,tx,id,ct);var row=await ReadAsync(c,tx,id,ct);Owner(row,actor);
        if(row.Status!=RecruitmentStatus.Draft)throw new DomainException("Conflict","Hanya draft yang dapat dipublikasikan.");
        Validate(row.Data,true,clock.GetUtcNow());
        await using var cmd=Db.Command(c,tx,"UPDATE Recruitments SET Status=1,PublishedAtUtc=@now,UpdatedAtUtc=@now WHERE Id=@id",("now",clock.GetUtcNow()),("id",id));await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task CloseAsync(int id,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await LockAsync(c,tx,id,ct);var row=await ReadAsync(c,tx,id,ct);Owner(row,actor);
        if(row.Status!=RecruitmentStatus.Open)throw new DomainException("Conflict","Hanya rekrutmen dibuka yang dapat ditutup.");
        await using var cmd=Db.Command(c,tx,"UPDATE Recruitments SET Status=2,UpdatedAtUtc=@now WHERE Id=@id",("now",clock.GetUtcNow()),("id",id));await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task DeleteDraftAsync(int id,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        await LockAsync(c,tx,id,ct);var row=await ReadAsync(c,tx,id,ct);Owner(row,actor);
        if(row.Status!=RecruitmentStatus.Draft)throw new DomainException("Conflict","Posting terpublikasi tidak dapat dihapus.");
        await using var cmd=Db.Command(c,tx,"DELETE PositionSkills WHERE PositionId IN(SELECT Id FROM Positions WHERE RecruitmentId=@id); DELETE Positions WHERE RecruitmentId=@id; DELETE Bookmarks WHERE RecruitmentId=@id; DELETE Recruitments WHERE Id=@id;",("id",id));await cmd.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<RecruitmentDetail> GetAsync(int id,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);var row=await ReadAsync(c,null,id,ct);
        if(row.Status==RecruitmentStatus.Draft&&row.OwnerId!=actor)throw new DomainException("NotFound","Rekrutmen tidak ditemukan.");
        var positions=await PositionsAsync(c,null,id,ct);var owner=await new ProfileService(connections,session,clock).GetAsync(row.OwnerId,ct);
        await using var count=Db.Command(c,null,"SELECT COUNT(*) FROM Applications WHERE RecruitmentId=@id",("id",id));var hasApps=Convert.ToInt32(await count.ExecuteScalarAsync(ct))>0;
        return new(id,row.OwnerId,owner,row.Data,row.Status,row.PublishedAtUtc,row.UpdatedAtUtc,positions.Any(p=>RecruitmentRules.CanApply(row.Status,row.Data.DeadlineUtc,p.AcceptedCount,p.Capacity,clock.GetUtcNow())),positions,hasApps);
    }
    public async Task<Page<RecruitmentSummary>> ListMineAsync(int page,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);
        var ids=new List<int>();int total;
        await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Recruitments WHERE OwnerId=@actor",("actor",actor)))total=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        page=Math.Clamp(page,1,Math.Max(1,(total+11)/12));
        await using(var cmd=Db.Command(c,null,"SELECT Id FROM Recruitments WHERE OwnerId=@actor ORDER BY UpdatedAtUtc DESC,Id DESC OFFSET @offset ROWS FETCH NEXT 12 ROWS ONLY",("actor",actor),("offset",(page-1)*12)))
        await using(var r=await cmd.ExecuteReaderAsync(ct))while(await r.ReadAsync(ct))ids.Add(r.GetInt32(0));
        var rows=new List<RecruitmentSummary>();foreach(var id in ids){var d=await GetAsync(id,ct);rows.Add(new(d.Id,d.Data.Title,d.Data.Category,d.PublishedAtUtc,d.Data.DeadlineUtc,d.Status,d.IsAvailable,false,d.Positions.Select(p=>p.Name).ToArray()){Positions=d.Positions});}
        return new(rows,total,page);
    }
}
