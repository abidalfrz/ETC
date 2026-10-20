using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Microsoft.Data.SqlClient;
namespace Etc.Desktop.Features.Profiles;
public interface IProfileService
{
    Task<PublicProfile> GetAsync(int studentId,CancellationToken ct=default);
    Task SaveMineAsync(ProfileInput input,CancellationToken ct=default);
    Task<IReadOnlyList<SkillOption>> ListSkillsAsync(CancellationToken ct=default);
    Task<bool> IsMineCompleteAsync(CancellationToken ct=default);
}
public sealed class ProfileService(SqlConnectionFactory connections,SessionState session,TimeProvider clock) : IProfileService
{
    public async Task<PublicProfile> GetAsync(int studentId,CancellationToken ct=default)
    {
        session.RequireStudentId();PublicProfile profile;
        await using var c=await connections.OpenAsync(ct);
        await using(var cmd=Db.Command(c,null,"SELECT Id,FullName,Cohort,Bio,Experience,PortfolioUrl,LinkedInUrl,Photo,IsDemo FROM Students WHERE Id=@id",("id",studentId)))
        await using(var r=await cmd.ExecuteReaderAsync(ct))
        {
            if(!await r.ReadAsync(ct))throw new DomainException("NotFound","Profil tidak ditemukan.");
            profile=new(r.GetInt32(0),r.GetString(1),r.IsDBNull(2)?null:r.GetInt32(2),r.GetString(3),r.GetString(4),r.IsDBNull(5)?null:r.GetString(5),r.IsDBNull(6)?null:r.GetString(6),[],r[7] as byte[],r.GetBoolean(8));
        }
        var skills=new List<SkillOption>();
        await using(var cmd=Db.Command(c,null,"SELECT s.Id,s.Name FROM Skills s JOIN StudentSkills ss ON ss.SkillId=s.Id WHERE ss.StudentId=@id ORDER BY s.Name",("id",studentId)))
        await using(var r=await cmd.ExecuteReaderAsync(ct))while(await r.ReadAsync(ct))skills.Add(new(r.GetInt32(0),r.GetString(1)));
        return profile with{Skills=skills};
    }
    public async Task SaveMineAsync(ProfileInput input,CancellationToken ct=default)
    {
        var id=session.RequireStudentId();var name=Guard.Text(input.FullName,"Nama lengkap",120);
        var bio=Guard.Text(input.Bio,"Bio",2000,false);var experience=Guard.Text(input.Experience,"Pengalaman",10000,false);
        if(input.Cohort<1000||input.Cohort>clock.GetUtcNow().Year||input.SkillIds.Count==0)
            throw new DomainException("Validation","Lengkapi angkatan yang valid dan minimal satu skill.");
        Guard.Url(input.PortfolioUrl);Guard.Url(input.LinkedInUrl);
        if(input.Photo is not null)PhotoValidator.Validate(input.Photo);
        await using var c=await connections.OpenAsync(ct);await using var tx=(SqlTransaction)await c.BeginTransactionAsync(ct);
        try
        {
            await using(var cmd=Db.Command(c,tx,"UPDATE Students SET FullName=@name,Cohort=@cohort,Bio=@bio,Experience=@experience,PortfolioUrl=@portfolio,LinkedInUrl=@linkedin,Photo=@photo WHERE Id=@id; DELETE StudentSkills WHERE StudentId=@id;",("name",name),("cohort",input.Cohort),("bio",bio),("experience",experience),("portfolio",input.PortfolioUrl),("linkedin",input.LinkedInUrl),("photo",input.Photo),("id",id)))
            {
                cmd.Parameters["@photo"].SqlDbType=System.Data.SqlDbType.VarBinary;
                await cmd.ExecuteNonQueryAsync(ct);
            }
            foreach(var skill in input.SkillIds.Distinct())
            {
                await using var cmd=Db.Command(c,tx,"INSERT StudentSkills VALUES(@id,@skill)",("id",id),("skill",skill));await cmd.ExecuteNonQueryAsync(ct);
            }
            await tx.CommitAsync(ct);
            session.UpdateName(id,name);
        }
        catch(SqlException e)when(e.Number==547){throw new DomainException("Validation","Skill yang dipilih tidak tersedia.");}
    }
    public async Task<IReadOnlyList<SkillOption>> ListSkillsAsync(CancellationToken ct=default)
    {
        var result=new List<SkillOption>();await using var c=await connections.OpenAsync(ct);
        await using var cmd=Db.Command(c,null,"SELECT Id,Name FROM Skills ORDER BY Name");
        await using var r=await cmd.ExecuteReaderAsync(ct);while(await r.ReadAsync(ct))result.Add(new(r.GetInt32(0),r.GetString(1)));return result;
    }
    public async Task<bool> IsMineCompleteAsync(CancellationToken ct=default)
    {
        var id=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);
        await using var cmd=Db.Command(c,null,"SELECT COUNT(*) FROM Students s WHERE Id=@id AND LEN(FullName)>0 AND Cohort IS NOT NULL AND EXISTS(SELECT 1 FROM StudentSkills WHERE StudentId=s.Id)",("id",id));
        return Convert.ToInt32(await cmd.ExecuteScalarAsync(ct))==1;
    }
}
