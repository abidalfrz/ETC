using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Recruitments;
namespace Etc.Desktop.Features.Bookmarks;
public interface IBookmarkService
{
    Task SetAsync(int recruitmentId,bool saved,CancellationToken ct=default);
    Task<Page<RecruitmentSummary>> ListMineAsync(int page,CancellationToken ct=default);
    Task<bool> IsSavedAsync(int recruitmentId,CancellationToken ct=default);
}
public sealed class BookmarkService(SqlConnectionFactory connections,SessionState session,TimeProvider clock):IBookmarkService
{
    public async Task<bool> IsSavedAsync(int recruitmentId,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);await using var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Bookmarks WHERE StudentId=@actor AND RecruitmentId=@id",("actor",actor),("id",recruitmentId));return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))>0;
    }
    public async Task SetAsync(int recruitmentId,bool saved,CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);
        if(saved)
        {
            await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Recruitments WHERE Id=@id AND (Status<>0 OR OwnerId=@actor)",("id",recruitmentId),("actor",actor)))
                if(Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==0)throw new DomainException("NotFound","Rekrutmen tidak ditemukan.");
            await using var insert=Db.Command(c,null,"INSERT Bookmarks(StudentId,RecruitmentId) SELECT @actor,@id WHERE NOT EXISTS(SELECT 1 FROM Bookmarks WITH(UPDLOCK,HOLDLOCK) WHERE StudentId=@actor AND RecruitmentId=@id)",("id",recruitmentId),("actor",actor));await insert.ExecuteNonQueryAsync(ct);
        }
        else
        {await using var cmd=Db.Command(c,null,"DELETE Bookmarks WHERE StudentId=@actor AND RecruitmentId=@id",("id",recruitmentId),("actor",actor));await cmd.ExecuteNonQueryAsync(ct);}
    }
    public Task<Page<RecruitmentSummary>> ListMineAsync(int page,CancellationToken ct=default)=>new RecruitmentQueries(connections,session,clock).ReadAsync(new("",null,null,null,false,page),true,ct);
}
