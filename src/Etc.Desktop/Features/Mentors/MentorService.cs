using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Mentors;
public interface IMentorService
{
    Task<Page<MentorSummary>> SearchAsync(string search,int page,CancellationToken ct=default);
    Task<MentorDetail> GetAsync(int mentorId,CancellationToken ct=default);
}
public sealed class MentorService(SqlConnectionFactory connections,SessionState session):IMentorService
{
    private const string Projection="m.Id,m.Name,m.Photo,m.IsDemo,m.Cohort,m.StudentNumber,m.Focus,m.Experience,JSON_QUERY((SELECT s.Id,s.Name FROM MentorSkills ms JOIN Skills s ON s.Id=ms.SkillId WHERE ms.MentorId=m.Id ORDER BY s.Name FOR JSON PATH)) AS Skills";
    public async Task<Page<MentorSummary>> SearchAsync(string search,int page,CancellationToken ct=default)
    {
        session.RequireStudentId();search=Guard.Text(search,"Pencarian mentor",200,false);
        var pattern="%"+search.Replace("~","~~").Replace("%","~%").Replace("_","~_").Replace("[","~[")+"%";
        const string where="m.Name LIKE @search ESCAPE '~' OR EXISTS(SELECT 1 FROM MentorSkills ms JOIN Skills s ON s.Id=ms.SkillId WHERE ms.MentorId=m.Id AND s.Name LIKE @search ESCAPE '~')";
        await using var c=await connections.OpenAsync(ct);int total;
        await using(var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Mentors m WHERE "+where,("search",pattern)))total=Convert.ToInt32(await cmd.ExecuteScalarAsync(ct));
        page=Math.Clamp(page,1,Math.Max(1,(total+11)/12));
        await using var query=Db.Command(c,null,"SELECT "+Projection+" FROM Mentors m WHERE "+where+" ORDER BY m.Name,m.Id OFFSET @offset ROWS FETCH NEXT 12 ROWS ONLY FOR JSON PATH",("search",pattern),("offset",(page-1)*12));
        return new(await Db.JsonAsync<List<MentorSummary>>(query,ct)??[],total,page);
    }
    public async Task<MentorDetail> GetAsync(int mentorId,CancellationToken ct=default)
    {
        session.RequireStudentId();await using var c=await connections.OpenAsync(ct);
        await using var query=Db.Command(c,null,"SELECT JSON_QUERY((SELECT "+Projection+" FOR JSON PATH,WITHOUT_ARRAY_WRAPPER)) AS Summary,m.Experience,m.Achievements FROM Mentors m WHERE m.Id=@id FOR JSON PATH,WITHOUT_ARRAY_WRAPPER",("id",mentorId));
        return await Db.JsonAsync<MentorDetail>(query,ct)??throw new DomainException("NotFound","Mentor tidak ditemukan.");
    }
}
