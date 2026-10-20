using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class ApplicantReviewTests(DatabaseFixture db)
{
    [Fact]public async Task PositionFilterCountsOnlyMatchingApplicantsAndStillRequiresOwner()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var detail=await t.Recruitments.GetAsync(id);
        var svc=new ApplicationService(db.Factory,t.Session,t.Clock);t.Session.SignIn(t.B);
        await svc.SubmitAsync(new(id,detail.Positions[0].Id,"Pesan",null));t.Session.SignIn(t.A);
        var matching=await svc.ListPositionApplicantsAsync(id,detail.Positions[0].Id,1);
        Assert.Equal(1,matching.Total);Assert.Single(matching.Items);
        Assert.Empty((await svc.ListPositionApplicantsAsync(id,int.MaxValue,1)).Items);
        t.Session.SignIn(t.B);await Assert.ThrowsAsync<DomainException>(()=>svc.ListPositionApplicantsAsync(id,detail.Positions[0].Id,1));
    }
    [Fact]public async Task OnlyOwnerCanDecideAndClosedRecruitmentCanReviewExistingPending()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0].Id;var svc=new ApplicationService(db.Factory,t.Session,t.Clock);
        t.Session.SignIn(t.B);var app=await svc.SubmitAsync(new(id,pos,"Pesan",null));
        await Assert.ThrowsAsync<DomainException>(()=>svc.DecideAsync(app,ApplicationStatus.Accepted));
        await Assert.ThrowsAsync<DomainException>(()=>svc.ListApplicantsAsync(id,1));
        await new ProfileService(db.Factory,t.Session,t.Clock).SaveMineAsync(new("Nadia terbaru",2024,"","",null,null,[t.SkillId],null));
        t.Session.SignIn(t.A);await t.Recruitments.CloseAsync(id);Assert.Equal("Nadia terbaru",(await svc.ListApplicantsAsync(id,1)).Items.Single().Applicant.FullName);
        var old=await t.Recruitments.GetAsync(id);
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.UpdatePublishedAsync(id,old.Data with{Positions=[old.Data.Positions[0] with{Capacity=2}]}));
        await svc.DecideAsync(app,ApplicationStatus.Accepted);
        await Assert.ThrowsAsync<DomainException>(()=>svc.DecideAsync(app,ApplicationStatus.Rejected));
        t.Session.SignIn(t.B);Assert.Equal(ApplicationStatus.Accepted,(await svc.ListMineAsync(1)).Items.Single().Status);
        await Assert.ThrowsAsync<DomainException>(()=>svc.CancelAsync(app));
    }
}
