using Etc.Desktop.Features.Mentors;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class MentorTests(DatabaseFixture db)
{
    [Fact]public async Task SearchMatchesNameOrSkillAndDemoIsLabelled()
    {
        var t=new Scenario(db);await t.InitializeAsync();
        await db.ExecuteAsync($"INSERT Mentors(Name,Experience,Achievements,IsDemo) VALUES(N'Mentor demonstrasi',N'Pengalaman simulasi',N'Pencapaian simulasi',1); INSERT MentorSkills SELECT MAX(Id),{t.SkillId} FROM Mentors;");
        var svc=new MentorService(db.Factory,t.Session);var rows=await svc.SearchAsync("Mentor",1);Assert.Single(rows.Items);Assert.True(rows.Items[0].IsDemo);
        var skill=rows.Items[0].Skills.Single().Name;Assert.Single((await svc.SearchAsync(skill,1)).Items);
        Assert.Equal("Pencapaian simulasi",(await svc.GetAsync(rows.Items[0].Id)).Achievements);
        Assert.Empty((await svc.SearchAsync("tak-ada",1)).Items);
    }
}
