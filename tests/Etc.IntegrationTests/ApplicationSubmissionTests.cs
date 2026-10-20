using Etc.Desktop.Features.Applications;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class ApplicationSubmissionTests(DatabaseFixture db)
{
    [Fact]public async Task SelfApplicationRejectedAndIncompleteProfileRejected()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0].Id;
        var svc=new ApplicationService(db.Factory,t.Session,t.Clock);
        await Assert.ThrowsAsync<DomainException>(()=>svc.SubmitAsync(new(id,pos,"Saya ingin bergabung",null)));
        t.Session.SignIn(t.B);await db.ExecuteAsync($"UPDATE Students SET Cohort=NULL WHERE Id={t.B.Id}");
        Assert.Equal("Validation",(await Assert.ThrowsAsync<DomainException>(()=>svc.SubmitAsync(new(id,pos,"Pesan",null)))).Code);
    }
    [Fact]public async Task DuplicateAcrossPositionsRejectedEvenAfterCancellation()
    {
        var t=new Scenario(db);await t.InitializeAsync();var input=t.Input();
        var id=await t.Recruitments.SaveDraftAsync(null,input with{Positions=[..input.Positions,new(null,"UI/UX",1,[t.SkillId])]});await t.Recruitments.PublishAsync(id);
        var positions=(await t.Recruitments.GetAsync(id)).Positions;t.Session.SignIn(t.B);var svc=new ApplicationService(db.Factory,t.Session,t.Clock);
        var app=await svc.SubmitAsync(new(id,positions[0].Id,"Pesan",null));await svc.CancelAsync(app);
        Assert.True(await svc.HasAppliedAsync(id));
        Assert.Equal(ApplicationStatus.Cancelled,(await svc.ListMineAsync(1)).Items.Single().Status);
        Assert.Equal("Conflict",(await Assert.ThrowsAsync<DomainException>(()=>svc.SubmitAsync(new(id,positions[1].Id,"Pesan lain",null)))).Code);
        t.Session.SignIn(t.C);await Assert.ThrowsAsync<DomainException>(()=>svc.CancelAsync(app));
    }
    [Fact]public async Task DeadlineCrossedBetweenDetailAndSubmitIsRejected()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var detail=await t.Recruitments.GetAsync(id);t.Session.SignIn(t.B);
        t.Clock.Now=detail.Data.DeadlineUtc!.Value;
        Assert.Equal("Unavailable",(await Assert.ThrowsAsync<DomainException>(()=>new ApplicationService(db.Factory,t.Session,t.Clock).SubmitAsync(new(id,detail.Positions[0].Id,"Pesan",null)))).Code);
    }
    [Fact]public async Task DoubleSubmitCreatesOneRow()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0].Id;t.Session.SignIn(t.B);
        var svc=new ApplicationService(db.Factory,t.Session,t.Clock);
        async Task<bool> Submit(){try{await svc.SubmitAsync(new(id,pos,"Pesan",null));return true;}catch(DomainException){return false;}}
        var result=await Task.WhenAll(Submit(),Submit());Assert.Single(result.Where(x=>x));Assert.Single((await svc.ListMineAsync(1)).Items);
    }
}
