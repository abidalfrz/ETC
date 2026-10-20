using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class ProfileTests(DatabaseFixture db)
{
    [Fact]public async Task InvalidSkillRollsBackAllProfileChanges()
    {
        var t=new Scenario(db);await t.InitializeAsync();var service=new ProfileService(db.Factory,t.Session,t.Clock);
        var before=await service.GetAsync(t.A.Id);
        await Assert.ThrowsAsync<DomainException>(()=>service.SaveMineAsync(new("Nama gagal",2024,"Bio gagal","",null,null,[t.SkillId,int.MaxValue],null)));
        var after=await service.GetAsync(t.A.Id);Assert.Equal(before.FullName,after.FullName);Assert.Equal(before.Bio,after.Bio);Assert.Equal(before.Skills,after.Skills);
    }
    [Fact] public async Task SaveMineCannotChangeAnotherStudentAndUnicodeRoundTrips()
    {
        await db.ResetAsync();var s=new SessionState();var accounts=new AccountService(db.Factory,s);
        var a=await accounts.RegisterAsync("Raka","a@example.test","Etc-Test-Pass!2026");
        var b=await accounts.RegisterAsync("Nadia","b@example.test","Etc-Test-Pass!2026");s.SignIn(a);
        var svc=new ProfileService(db.Factory,s,TimeProvider.System);
        Assert.False(await svc.IsMineCompleteAsync());
        var skill=(await svc.ListSkillsAsync())[0];
        await svc.SaveMineAsync(new("Raka 日本語",2024,"Bio","Pengalaman",null,null,[skill.Id],null));
        Assert.Equal("Raka 日本語",(await svc.GetAsync(a.Id)).FullName);
        Assert.Equal("Nadia",(await svc.GetAsync(b.Id)).FullName);
        Assert.True(await svc.IsMineCompleteAsync());
        Assert.Equal("Pengalaman",(await svc.GetAsync(a.Id)).Experience);
    }
    [Fact] public async Task MissingNameCohortOrSkillBlocksCompletion()
    {
        await db.ResetAsync();var s=new SessionState();var a=await new AccountService(db.Factory,s).RegisterAsync("Raka","a@example.test","Etc-Test-Pass!2026");s.SignIn(a);
        var service=new ProfileService(db.Factory,s,TimeProvider.System);
        foreach(var input in new[]{new ProfileInput("",2024,"","",null,null,[1],null),new("Raka",9999,"","",null,null,[1],null),new("Raka",2024,"","",null,null,[],null)})
            Assert.Equal("Validation",(await Assert.ThrowsAsync<DomainException>(()=>service.SaveMineAsync(input))).Code);
    }
}
