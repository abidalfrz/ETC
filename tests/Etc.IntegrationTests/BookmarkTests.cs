using Etc.Desktop.Features.Bookmarks;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class BookmarkTests(DatabaseFixture db)
{
    [Fact]public async Task OwnIncompleteDraftIsListedAndCountedWithoutLeakingToOtherAccounts()
    {
        var t=new Scenario(db);await t.InitializeAsync();var input=t.Input() with{Positions=[]};var id=await t.Recruitments.SaveDraftAsync(null,input);
        var bookmarks=new BookmarkService(db.Factory,t.Session,t.Clock);await bookmarks.SetAsync(id,true);
        Assert.Equal(id,(await bookmarks.ListMineAsync(1)).Items.Single().Id);
        Assert.Equal(1,(await new Etc.Desktop.Features.Dashboard.DashboardService(db.Factory,t.Session,t.Clock).GetMineAsync()).Stats.Bookmarks);
        Assert.Empty((await new Etc.Desktop.Features.Recruitments.RecruitmentQueries(db.Factory,t.Session,t.Clock).SearchAsync(new("",null,null,null,false,1))).Items);
        t.Session.SignIn(t.B);Assert.Empty((await bookmarks.ListMineAsync(1)).Items);await Assert.ThrowsAsync<DomainException>(()=>bookmarks.SetAsync(id,true));
    }
    [Fact]public async Task BookmarkSetIsIdempotentAndAccountScoped()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var svc=new BookmarkService(db.Factory,t.Session,t.Clock);
        t.Session.SignIn(t.B);await svc.SetAsync(id,true);await svc.SetAsync(id,true);Assert.Single((await svc.ListMineAsync(1)).Items);
        Assert.True(await svc.IsSavedAsync(id));t.Session.SignIn(t.C);Assert.Empty((await svc.ListMineAsync(1)).Items);Assert.False(await svc.IsSavedAsync(id));
        t.Session.SignIn(t.A);await t.Recruitments.CloseAsync(id);
        t.Session.SignIn(t.B);Assert.Single((await svc.ListMineAsync(1)).Items);await svc.SetAsync(id,false);Assert.Empty((await svc.ListMineAsync(1)).Items);
        Assert.NotNull(await t.Recruitments.GetAsync(id));
    }
    [Fact]public async Task CannotBookmarkAnotherStudentsDraft()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.Recruitments.SaveDraftAsync(null,t.Input());t.Session.SignIn(t.B);
        await Assert.ThrowsAsync<DomainException>(()=>new BookmarkService(db.Factory,t.Session,t.Clock).SetAsync(id,true));
    }
}
