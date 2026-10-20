using Etc.Desktop.Demo;
using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class DemoSeederTests(DatabaseFixture db)
{
    [Fact]public async Task SeedWithoutPasswordCreatesDemoAccountsWithDocumentedDefault()
    {
        await db.ResetAsync();
        await new DemoSeeder(db.Factory,new TestClock()).SeedAsync();
        foreach(var email in new[]{"raka.demo@example.test","nadia.demo@example.test","dimas.demo@example.test"})
            await new AccountService(db.Factory,new()).LoginAsync(email,"Etc-Demo!2026");
    }
    [Fact]public async Task SeedIsIdempotentAndDoesNotOverwriteExistingRecords()
    {
        await db.ResetAsync();var seeder=new DemoSeeder(db.Factory,new TestClock());await seeder.SeedAsync("Etc-Demo-Test!2026");
        var count=Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Recruitments"));Assert.True(count>=7);
        await db.ExecuteAsync("UPDATE Students SET Bio=N'Bio telah diedit' WHERE Email='raka.demo@example.test'");
        await seeder.SeedAsync("Another-password!2026");Assert.Equal(count,Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Recruitments")));
        Assert.Equal("Bio telah diedit",await db.ScalarAsync("SELECT Bio FROM Students WHERE Email='raka.demo@example.test'"));
        Assert.NotEqual("Etc-Demo-Test!2026",await db.ScalarAsync("SELECT TOP 1 PasswordHash FROM Students"));
        await new AccountService(db.Factory,new()).LoginAsync("raka.demo@example.test","Etc-Demo-Test!2026");
        Assert.Equal(4,Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(DISTINCT Status) FROM Applications")));
        Assert.Equal(3,Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(DISTINCT Status) FROM Recruitments")));
        Assert.Equal(15,Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Mentors WHERE IsDemo=0")));
    }
    [Fact]public async Task SuppliedMentorsHaveReadablePhotosAndSearchableSkillsAcrossPages()
    {
        await db.ResetAsync();await new DemoSeeder(db.Factory,new TestClock()).SeedAsync("Etc-Demo-Test!2026");
        var session=new SessionState();await new AccountService(db.Factory,session).LoginAsync("raka.demo@example.test","Etc-Demo-Test!2026");
        var service=new Etc.Desktop.Features.Mentors.MentorService(db.Factory,session);
        var first=await service.SearchAsync("",1);var second=await service.SearchAsync("",2);
        Assert.Equal(15,first.Total);Assert.Equal(12,first.Items.Count);Assert.Equal(3,second.Items.Count);
        foreach(var row in first.Items.Concat(second.Items))
        {
            Assert.False(row.IsDemo);Assert.NotEmpty(row.Photo!);Etc.Desktop.Features.Profiles.PhotoValidator.Validate(row.Photo!);
            Assert.NotEmpty(row.Skills);
        }
        Assert.Equal(5,(await service.SearchAsync("UI/UX Design",1)).Total);
        Assert.Equal(3,(await service.SearchAsync("Competitive Programming",1)).Total);
        Assert.Single((await service.SearchAsync("Malfino Muhammad Willianz",1)).Items);
        var rayyaan=Assert.Single((await service.SearchAsync("Muhammad Rayyaan",1)).Items);
        Assert.Equal(2022,rayyaan.Cohort);Assert.Equal("5025221047",rayyaan.StudentNumber);Assert.Equal("Competition, Project",rayyaan.Focus);
        Assert.Equal("https://tinyurl.com/PortofolioRayyaan",(await service.GetAsync(rayyaan.Id)).Experience);
        var opened=new List<Uri>();var links=new ExternalLinkService(uri=>{opened.Add(uri);return Task.FromResult(true);});
        var profile=new Etc.Desktop.Features.Mentors.MentorDetailViewModel(service,session,rayyaan.Id,links,MentorSeedData.FormUrl);
        await profile.LoadAsync();await profile.PortfolioCommand.ExecuteAsync(null);await profile.RequestMentorCommand.ExecuteAsync(null);
        Assert.False(profile.HasError,profile.ErrorMessage);
        Assert.Equal(new[]{"https://tinyurl.com/PortofolioRayyaan","https://forms.gle/di9JYWdXJ5822Kgq7"},opened.Select(x=>x.AbsoluteUri));
    }
    [Fact]public async Task ReseedingRefreshesSuppliedProfilesAndKeepsUnrelatedMentors()
    {
        await db.ResetAsync();var seeder=new DemoSeeder(db.Factory,new TestClock());await seeder.SeedAsync("Etc-Demo-Test!2026");
        await db.ExecuteAsync("UPDATE Mentors SET Photo=NULL,Experience=N'outdated' WHERE Name=N'Dian Kusumawati'; INSERT Mentors(Name,Experience,Achievements) VALUES(N'Mentor tambahan',N'Pengalaman asli',N'');");
        await seeder.SeedAsync("Etc-Demo-Test!2026");
        Assert.Equal(16,Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Mentors")));
        Assert.True(Convert.ToInt32(await db.ScalarAsync("SELECT DATALENGTH(Photo) FROM Mentors WHERE Name=N'Dian Kusumawati'"))>0);
        Assert.Equal("Pengalaman asli",await db.ScalarAsync("SELECT Experience FROM Mentors WHERE Name=N'Mentor tambahan'"));
        Assert.Equal("Gemastik UX & PPL, PKM K, Hackathon Gov-AI, BP, KTI",await db.ScalarAsync("SELECT Experience FROM Mentors WHERE Name=N'Dian Kusumawati'"));
    }
    [Fact]public async Task SeedRejectsEmptyPasswordOverride()
    {
        Assert.Equal("Validation",(await Assert.ThrowsAsync<DomainException>(()=>new DemoSeeder(db.Factory,new TestClock()).SeedAsync(""))).Code);
    }
    [Fact]public async Task SeedUsesSkillsFromEachRecruitmentTemplate()
    {
        await db.ResetAsync();await new DemoSeeder(db.Factory,new TestClock()).SeedAsync("Etc-Demo-Test!2026");
        Assert.Equal(1,await SkillCount("Hology 9.0 (Holomine)","Python"));
        Assert.Equal(0,await SkillCount("Hology 9.0 (Holomine)","C#"));
        Assert.Equal(1,await SkillCount("Aplikasi inventaris laboratorium","CSS"));
        Assert.Equal(1,await SkillCount("Aplikasi inventaris laboratorium","HTML"));
        Assert.Equal(1,await SkillCount("Portal dokumentasi kegiatan","JavaScript"));
    }
    private async Task<int> SkillCount(string title,string skill)=>Convert.ToInt32(await db.ScalarAsync($"SELECT COUNT(*) FROM Recruitments r JOIN Positions p ON p.RecruitmentId=r.Id JOIN PositionSkills ps ON ps.PositionId=p.Id JOIN Skills s ON s.Id=ps.SkillId WHERE r.Title=N'{title}' AND s.Name=N'{skill}'"));
}
