using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class PageViewModelTests
{
    [Fact]public async Task RetryRepeatsFailedActionInsteadOfReloadingForm()
    {
        var p=new Probe(new());int attempts=0;
        await p.Save(_=>{if(++attempts==1)throw new DomainException("Unavailable","Koneksi gagal");return Task.CompletedTask;});
        Assert.True(p.HasError);await p.RetryCommand.ExecuteAsync(null);Assert.Equal(2,attempts);Assert.False(p.HasError);
    }
    private sealed class Probe(SessionState s):PageViewModel(s,"Probe")
    {
        public string Value {get;private set;}="";
        public Task Read(Task<string> value)=>ReadAsync(_=>value,x=>Value=x);
        public Task Read(Func<CancellationToken,Task<string>> read,CancellationToken ct=default)=>ReadAsync(read,x=>Value=x,ct);
        public Task Save(Func<CancellationToken,Task> work)=>RunAsync(work);
    }
    [Fact]public async Task RetryAfterCommittedMutationRetriesOnlyItsFailedRead()
    {
        var p=new Probe(new());int writes=0,reads=0;
        Task<string> Read(CancellationToken ct){if(++reads==1)throw new DomainException("Unavailable","Lecture koneksi gagal");return Task.FromResult("Data terbaru");}
        await p.Save(async _=>{writes++;await p.Read(Read);});Assert.True(p.HasError);
        await p.RetryCommand.ExecuteAsync(null);Assert.False(p.HasError);Assert.Equal(1,writes);Assert.Equal(2,reads);Assert.Equal("Data terbaru",p.Value);
    }
    [Fact]public async Task LateReadCannotOverwriteNewerResult()
    {
        var p=new Probe(new());var first=new TaskCompletionSource<string>();var old=p.Read(first.Task);
        await p.Read(Task.FromResult("new"));first.SetResult("old");await old;Assert.Equal("new",p.Value);
    }
    [Fact]public async Task ResponseAfterLogoutIsDiscarded()
    {
        var s=new SessionState();s.SignIn(new(1,"Raka","a@example.test"));var p=new Probe(s);var value=new TaskCompletionSource<string>();var read=p.Read(value.Task);
        s.SignOut();value.SetResult("private");await read;Assert.Equal("",p.Value);
    }
    [Fact]public async Task FailedSaveHasSafeErrorAndBusyGuard()
    {
        var p=new Probe(new());await p.Save(_=>throw new DomainException("Validation","Input belum lengkap"));Assert.Equal("Input belum lengkap",p.ErrorMessage);Assert.False(p.IsBusy);
        var finish=new TaskCompletionSource();int calls=0;
        async Task Save(CancellationToken ct){calls++;await finish.Task;}
        var a=p.Save(Save);var b=p.Save(Save);finish.SetResult();await Task.WhenAll(a,b);Assert.Equal(1,calls);
    }
    [Fact]public async Task ReadAndWriteFailuresHaveDistinctSafeMessages()
    {
        var p=new Probe(new());
        await p.Read(Task.FromException<string>(new InvalidOperationException("private SQL details")));
        var readError=p.ErrorMessage;Assert.True(p.HasError);Assert.DoesNotContain("private SQL details",readError);
        await p.Save(_=>throw new InvalidOperationException("private SQL details"));
        Assert.True(p.HasError);Assert.DoesNotContain("private SQL details",p.ErrorMessage);Assert.NotEqual(readError,p.ErrorMessage);
    }
    [Fact]public async Task OldMutationCompletionDoesNotClearBusyStateOfReactivatedPage()
    {
        var p=new Probe(new());var oldFinish=new TaskCompletionSource();var currentFinish=new TaskCompletionSource();
        var old=p.Save(_=>oldFinish.Task);p.Deactivate();p.Activate();var current=p.Save(_=>currentFinish.Task);
        oldFinish.SetResult();await old;Assert.True(p.IsBusy);
        currentFinish.SetResult();await current;Assert.False(p.IsBusy);
    }
    [Fact]public async Task OldMutationFailureDoesNotSetErrorOnReactivatedPage()
    {
        var p=new Probe(new());var finish=new TaskCompletionSource();var old=p.Save(_=>finish.Task);
        p.Deactivate();p.Activate();finish.SetException(new InvalidOperationException());await old;
        Assert.False(p.HasError);
    }
    [Fact]public async Task CancelledReloadDoesNotReplaceCurrentRead()
    {
        var p=new Probe(new());var finish=new TaskCompletionSource<string>();var current=p.Read(finish.Task);
        using var cancelled=new CancellationTokenSource();cancelled.Cancel();
        await p.Read(_=>Task.FromResult("obsolete"),cancelled.Token);Assert.True(p.IsBusy);
        finish.SetResult("current");await current;Assert.Equal("current",p.Value);
    }
}
