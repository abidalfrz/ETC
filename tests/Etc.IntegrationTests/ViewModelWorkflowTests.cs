using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Bookmarks;
using Etc.Desktop.Features.Dashboard;
using Etc.Desktop.Features.Mentors;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class ViewModelWorkflowTests(DatabaseFixture db)
{
    [Theory][InlineData("closed")][InlineData("expired")][InlineData("full")]
    public async Task EditingUnavailableRecruitmentMetadataPreservesExactDeadline(string state)
    {
        var t=new Scenario(db);await t.InitializeAsync();var input=t.Input() with{DeadlineUtc=t.Clock.Now.AddDays(2).AddSeconds(37).AddTicks(12345)};
        var id=await t.Recruitments.SaveDraftAsync(null,input);await t.Recruitments.PublishAsync(id);
        if(state=="closed")await t.Recruitments.CloseAsync(id);
        else if(state=="expired")t.Clock.Now=input.DeadlineUtc!.Value.AddTicks(1);
        else{var position=(await t.Recruitments.GetAsync(id)).Positions[0].Id;t.Session.SignIn(t.B);var service=new ApplicationService(db.Factory,t.Session,t.Clock);var app=await service.SubmitAsync(new(id,position,"Pesan",null));t.Session.SignIn(t.A);await service.DecideAsync(app,ApplicationStatus.Accepted);}
        var profiles=new ProfileService(db.Factory,t.Session,t.Clock);var form=new RecruitmentFormViewModel(t.Recruitments,profiles,t.Session,id,(_,_)=>{});
        await form.LoadAsync();Assert.False(form.CanEditDeadline);form.Description="Deskripsi diperbarui setelah ditutup";await form.SaveCommand.ExecuteAsync(null);
        Assert.False(form.HasError,form.ErrorMessage);var saved=await t.Recruitments.GetAsync(id);Assert.Equal(input.DeadlineUtc,saved.Data.DeadlineUtc);Assert.Equal(form.Description,saved.Data.Description);
    }
    private static readonly ExternalLinkService Links=new(_=>Task.FromResult(true));
    [Fact]public async Task CreateApplyAcceptAndRefreshDashboardThroughViewModelCommands()
    {
        var t=new Scenario(db);await t.InitializeAsync();var profiles=new ProfileService(db.Factory,t.Session,t.Clock);int recruitment=0;
        var form=new RecruitmentFormViewModel(t.Recruitments,profiles,t.Session,0,(route,id)=>{Assert.Equal("detail",route);recruitment=id;});
        await form.LoadAsync();form.TitleText="Tim 日本語";form.ActivityName="Demo";form.Description="Deskripsi";form.Category="Proyek";form.Deadline=t.Clock.Now.AddDays(1);form.ContactChannel="Kontak contoh";form.Fee="Gratis";
        form.Positions[0].Name="Backend";form.Positions[0].Skills.First(s=>s.Id==t.SkillId).IsSelected=true;
        await form.PublishCommand.ExecuteAsync(null);Assert.False(form.HasError,form.ErrorMessage);Assert.True(recruitment>0);Assert.True(form.HasInfo);
        t.Session.SignIn(t.B);var applications=new ApplicationService(db.Factory,t.Session,t.Clock);var bookmarks=new BookmarkService(db.Factory,t.Session,t.Clock);
        var detail=new RecruitmentDetailViewModel(t.Recruitments,profiles,bookmarks,applications,t.Session,recruitment,(_,_)=>{},Links);
        await detail.LoadAsync();Assert.True(detail.CanApply);detail.Message="Halo, saya ingin berkontribusi.";
        await detail.SubmitCommand.ExecuteAsync(null);Assert.False(detail.HasError,detail.ErrorMessage);Assert.True(detail.HasApplied);Assert.False(detail.CanApply);Assert.True(detail.HasInfo);
        t.Session.SignIn(t.A);var applicants=new ApplicantsViewModel(applications,t.Recruitments,t.Session,recruitment,(_,_)=>{},(_,_)=>Task.FromResult(true),Links);
        await applicants.LoadAsync();await applicants.AcceptCommand.ExecuteAsync(null);Assert.False(applicants.HasError,applicants.ErrorMessage);Assert.Equal(ApplicationStatus.Accepted,applicants.SelectedApplicant?.Status);Assert.True(applicants.HasInfo);
        t.Session.SignIn(t.B);var dash=new DashboardViewModel(new DashboardService(db.Factory,t.Session,t.Clock),t.Session,(_,_)=>{});
        await dash.LoadAsync();Assert.Equal(1,dash.Stats.AcceptedApplications);Assert.Equal(0,dash.Stats.PendingApplications);
        var mine=new MyApplicationsViewModel(applications,t.Session,(_,_)=>{},(_,_)=>Task.FromResult(true));await mine.LoadAsync();Assert.False(mine.Items.Single().CanCancel);
    }
    [Fact]public async Task FailedProfileAndDraftSaveKeepInputs()
    {
        var t=new Scenario(db);await t.InitializeAsync();var profiles=new ProfileService(db.Factory,t.Session,t.Clock);
        var profile=new ProfileViewModel(profiles,t.Session,0,()=>Task.FromResult<byte[]?>(null),Links);await profile.LoadAsync();
        profile.FullName="Nama belum tersimpan";profile.Cohort="oops";await profile.SaveCommand.ExecuteAsync(null);
        Assert.True(profile.HasError);Assert.Equal("Nama belum tersimpan",profile.FullName);Assert.Equal("Raka",(await profiles.GetAsync(t.A.Id)).FullName);
        var form=new RecruitmentFormViewModel(t.Recruitments,profiles,t.Session,0,(_,_)=>{});await form.LoadAsync();form.TitleText="Draft tetap ada";form.Positions[0].Name="Backend";form.Positions[0].Capacity="bukan angka";
        await form.SaveCommand.ExecuteAsync(null);Assert.True(form.HasError);Assert.Equal("Draft tetap ada",form.TitleText);Assert.Equal("bukan angka",form.Positions[0].Capacity);
    }
    [Fact]public async Task CancelCommandRefreshesPendingHistory()
    {
        var t=new Scenario(db);await t.InitializeAsync();var rec=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(rec)).Positions[0].Id;
        t.Session.SignIn(t.B);var service=new ApplicationService(db.Factory,t.Session,t.Clock);await service.SubmitAsync(new(rec,pos,"Pesan",null));
        var vm=new MyApplicationsViewModel(service,t.Session,(_,_)=>{},(_,_)=>Task.FromResult(true));await vm.LoadAsync();await vm.CancelCommand.ExecuteAsync(vm.Items.Single());
        Assert.False(vm.HasError,vm.ErrorMessage);Assert.Equal(ApplicationStatus.Cancelled,vm.Items.Single().Status);Assert.True(vm.HasInfo);
    }
    [Fact]public async Task RecruitmentAndBookmarkFeedbackFollowsCommittedChangesAndNotCancelledConfirmation()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.Recruitments.SaveDraftAsync(null,t.Input());bool confirm=false;
        var mine=new MyRecruitmentsViewModel(t.Recruitments,t.Session,(_,_)=>{},(_,_)=>Task.FromResult(confirm));await mine.LoadAsync();
        await mine.PublishCommand.ExecuteAsync(mine.Items.Single());
        Assert.False(mine.HasInfo);Assert.Equal(RecruitmentStatus.Draft,(await t.Recruitments.GetAsync(id)).Status);
        confirm=true;await mine.PublishCommand.ExecuteAsync(mine.Items.Single());
        Assert.True(mine.HasInfo);Assert.Equal(RecruitmentStatus.Open,(await t.Recruitments.GetAsync(id)).Status);
        await mine.CloseCommand.ExecuteAsync(mine.Items.Single());
        Assert.True(mine.HasInfo);Assert.Equal(RecruitmentStatus.Closed,(await t.Recruitments.GetAsync(id)).Status);
        var draft=await t.Recruitments.SaveDraftAsync(null,t.Input("Draft dihapus"));await mine.LoadAsync();
        await mine.DeleteCommand.ExecuteAsync(mine.Items.Single(r=>r.Id==draft));Assert.True(mine.HasInfo);Assert.DoesNotContain(mine.Items,r=>r.Id==draft);
        var profiles=new ProfileService(db.Factory,t.Session,t.Clock);var saved=new BookmarkService(db.Factory,t.Session,t.Clock);
        var detail=new RecruitmentDetailViewModel(t.Recruitments,profiles,saved,new ApplicationService(db.Factory,t.Session,t.Clock),t.Session,id,(_,_)=>{},Links);await detail.LoadAsync();
        await detail.BookmarkCommand.ExecuteAsync(null);Assert.True(detail.HasInfo);Assert.True(await saved.IsSavedAsync(id));
        await detail.BookmarkCommand.ExecuteAsync(null);Assert.True(detail.HasInfo);Assert.False(await saved.IsSavedAsync(id));
        await saved.SetAsync(id,true);var bookmarks=new BookmarksViewModel(saved,t.Session,(_,_)=>{});await bookmarks.LoadAsync();
        await bookmarks.RemoveCommand.ExecuteAsync(id);Assert.True(bookmarks.HasInfo);Assert.False(await saved.IsSavedAsync(id));
    }
    [Theory][InlineData(null)][InlineData("javascript:alert(1)")]
    public void MissingOrInvalidMentorFormHasExplicitDisabledReason(string? url)
    {
        var vm=new MentorDetailViewModel(null!,new(),1,Links,url);Assert.False(vm.CanRequestMentor);Assert.Contains("belum tersedia",vm.FormReason);
    }
}
