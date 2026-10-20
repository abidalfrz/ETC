using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Bookmarks;
namespace Etc.UnitTests;
public sealed class SearchViewModelTests
{
    private sealed class Queries(Func<RecruitmentFilter,Task<Page<RecruitmentSummary>>> read):IRecruitmentQueries
    {public Task<Page<RecruitmentSummary>> SearchAsync(RecruitmentFilter f,CancellationToken ct=default)=>read(f);}
    private static RecruitmentSummary Row(string title)=>new(1,title,RecruitmentCategory.Project,null,null,RecruitmentStatus.Open,true,false,[]);
    [Fact]public async Task OldResponseCannotOverwriteNewSearch()
    {
        var old=new TaskCompletionSource<Page<RecruitmentSummary>>();int call=0;
        var q=new Queries(_=>++call==1?old.Task:Task.FromResult(new Page<RecruitmentSummary>([Row("hasil-terbaru")],1,1)));
        var vm=new SearchViewModel(q,null!,null!,new(),(_,_)=>{});
        var first=vm.LoadAsync();await vm.LoadAsync();old.SetResult(new([Row("old")],1,1));await first;
        Assert.Equal("hasil-terbaru",vm.Items.Single().Title);
    }
    [Fact]public void ChangingFilterResetsPageToOne()
    {
        var vm=new SearchViewModel(null!,null!,null!,new(),(_,_)=>{}){PageNumber=5};vm.SearchText="new";Assert.Equal(1,vm.PageNumber);
    }
    [Fact]public async Task BookmarkSuccessAppearsOnlyAfterWriteCompletes()
    {
        var finish=new TaskCompletionSource();
        var vm=new SearchViewModel(new Queries(_=>Task.FromResult(new Page<RecruitmentSummary>([],0,1))),null!,new Bookmarks(()=>finish.Task),new(),(_,_)=>{});
        var saving=vm.BookmarkCommand.ExecuteAsync(Row("Tim"));Assert.False(vm.HasInfo);
        finish.SetResult();await saving;Assert.True(vm.HasInfo);Assert.False(vm.HasError);
    }
    [Fact]public async Task FailedBookmarkWriteDoesNotShowSuccess()
    {
        var vm=new SearchViewModel(null!,null!,new Bookmarks(()=>throw new DomainException("Unavailable","Gagal menyimpan")),new(),(_,_)=>{}){InfoMessage="Hasil operasi sebelumnya"};
        await vm.BookmarkCommand.ExecuteAsync(Row("Tim"));Assert.True(vm.HasError);Assert.False(vm.HasInfo);
    }
    [Fact]public async Task CommittedBookmarkWithFailedRefreshRetainsSuccessAndRetryDoesNotRepeatWrite()
    {
        int writes=0,reads=0;
        var vm=new SearchViewModel(new Queries(_=>++reads==1?throw new DomainException("Unavailable","Gagal memuat daftar"):Task.FromResult(new Page<RecruitmentSummary>([],0,1))),null!,new Bookmarks(()=>{writes++;return Task.CompletedTask;}),new(),(_,_)=>{});
        await vm.BookmarkCommand.ExecuteAsync(Row("Tim"));Assert.True(vm.HasInfo);Assert.True(vm.HasError);
        await vm.RetryCommand.ExecuteAsync(null);Assert.False(vm.HasError);Assert.Equal(1,writes);Assert.Equal(2,reads);
    }
    [Fact]public async Task BookmarkCompletionAfterLogoutDoesNotPublishFeedback()
    {
        var session=new SessionState();session.SignIn(new(1,"Raka","a@example.test"));var finish=new TaskCompletionSource();
        var vm=new SearchViewModel(new Queries(_=>Task.FromResult(new Page<RecruitmentSummary>([],0,1))),null!,new Bookmarks(()=>finish.Task),session,(_,_)=>{});
        var saving=vm.BookmarkCommand.ExecuteAsync(Row("Tim"));session.SignOut();vm.Deactivate();finish.SetResult();await saving;
        Assert.False(vm.HasInfo);
    }
    [Fact]public async Task OldBookmarkCompletionDoesNotPublishOrReloadAfterHistoryPageReactivation()
    {
        var session=new SessionState();session.SignIn(new(1,"Raka","a@example.test"));var finish=new TaskCompletionSource();int reads=0;
        var vm=new SearchViewModel(new Queries(_=>{reads++;return Task.FromResult(new Page<RecruitmentSummary>([],0,1));}),null!,new Bookmarks(()=>finish.Task),session,(_,_)=>{});
        await vm.LoadAsync();var saving=vm.BookmarkCommand.ExecuteAsync(Row("Tim"));
        vm.Deactivate();vm.Activate();await vm.LoadAsync();finish.SetResult();await saving;
        Assert.False(vm.HasInfo);Assert.Equal(2,reads);
    }
    private sealed class Bookmarks(Func<Task> write):IBookmarkService
    {
        public Task SetAsync(int recruitmentId,bool saved,CancellationToken ct=default)=>write();
        public Task<Page<RecruitmentSummary>> ListMineAsync(int page,CancellationToken ct=default)=>throw new NotSupportedException();
        public Task<bool> IsSavedAsync(int recruitmentId,CancellationToken ct=default)=>throw new NotSupportedException();
    }
}
