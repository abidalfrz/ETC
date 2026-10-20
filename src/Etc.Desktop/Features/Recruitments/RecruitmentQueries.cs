using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Recruitments;
public sealed record RecruitmentFilter(string Search,RecruitmentCategory? Category,int? SkillId,string? PositionName,bool OnlyAvailable,int PageNumber);
public interface IRecruitmentQueries { Task<Page<RecruitmentSummary>> SearchAsync(RecruitmentFilter filter,CancellationToken ct=default); }
public sealed class RecruitmentQueries(SqlConnectionFactory connections,SessionState session,TimeProvider clock):IRecruitmentQueries
{
    public Task<Page<RecruitmentSummary>> SearchAsync(RecruitmentFilter filter,CancellationToken ct=default)=>ReadAsync(filter,false,ct);
    private static string Escape(string s)=>s.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[");
    internal async Task<Page<RecruitmentSummary>> ReadAsync(RecruitmentFilter f,bool bookmarks,CancellationToken ct)
    {
        var actor=session.RequireStudentId();Guard.Text(f.Search,"Pencarian",200,false);Guard.Text(f.PositionName,"Posisi",120,false);
        const string where="(r.Status<>0 OR (@bookmarks=1 AND r.OwnerId=@actor)) AND (@search='' OR r.Title LIKE @pattern ESCAPE '~' OR r.ActivityName LIKE @pattern ESCAPE '~') AND (@category IS NULL OR r.Category=@category) AND (@only=0 OR (r.Status=1 AND r.DeadlineUtc>@now)) AND (@bookmarks=0 OR EXISTS(SELECT 1 FROM Bookmarks b WHERE b.StudentId=@actor AND b.RecruitmentId=r.Id)) AND (@bookmarks=1 OR EXISTS(SELECT 1 FROM Positions p WHERE p.RecruitmentId=r.Id AND (@skill IS NULL OR EXISTS(SELECT 1 FROM PositionSkills ps WHERE ps.PositionId=p.Id AND ps.SkillId=@skill)) AND (@position='' OR p.Name LIKE @positionPattern ESCAPE '~') AND (@only=0 OR p.Capacity>(SELECT COUNT(*) FROM Applications a WHERE a.PositionId=p.Id AND a.Status=1))))";
        (string,object?)[] args=[("actor",actor),("search",f.Search.Trim()),("pattern","%"+Escape(f.Search.Trim())+"%"),("category",f.Category is null?null:(int)f.Category),("skill",f.SkillId),("position",f.PositionName??""),("positionPattern","%"+Escape(f.PositionName??"")+"%"),("only",f.OnlyAvailable),("now",clock.GetUtcNow()),("bookmarks",bookmarks)];
        await using var c=await connections.OpenAsync(ct);int total;
        await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Recruitments r WHERE "+where,args))total=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        var page=Math.Clamp(f.PageNumber,1,Math.Max(1,(total+11)/12));var ids=new List<int>();
        await using(var cmd=Db.Command(c,null,"SELECT r.Id FROM Recruitments r WHERE "+where+" ORDER BY r.PublishedAtUtc DESC,r.Id DESC OFFSET @offset ROWS FETCH NEXT 12 ROWS ONLY",[..args,("offset",(page-1)*12)]))
        await using(var r=await cmd.ExecuteReaderAsync(ct))while(await r.ReadAsync(ct))ids.Add(r.GetInt32(0));
        var saved=new HashSet<int>();await using(var cmd=Db.Command(c,null,"SELECT RecruitmentId FROM Bookmarks WHERE StudentId=@actor",("actor",actor)))
        await using(var r=await cmd.ExecuteReaderAsync(ct))while(await r.ReadAsync(ct))saved.Add(r.GetInt32(0));
        var result=new List<RecruitmentSummary>();var details=new RecruitmentService(connections,session,clock);
        foreach(var id in ids)
        {
            ct.ThrowIfCancellationRequested();var d=await details.GetAsync(id,ct);
            result.Add(new(id,d.Data.Title,d.Data.Category,d.PublishedAtUtc,d.Data.DeadlineUtc,d.Status,d.IsAvailable,saved.Contains(id),d.Positions.Select(p=>p.Name).ToArray()){Positions=d.Positions});
        }
        return new(result,total,page);
    }
}
