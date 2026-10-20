using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class ShellViewModelTests
{
    private sealed class Probe(SessionState s,string title):PageViewModel(s,title);
    [Fact]public void SuccessFeedbackSurvivesNavigationButDismissedFeedbackDoesNotReturn()
    {
        var session=new SessionState();session.SignIn(new(1,"Raka","a@example.test"));
        var shell=new ShellViewModel(session,(route,_)=>new Probe(session,route),(_,_)=>Task.FromResult(true));
        shell.Go("mine");shell.CurrentPage!.InfoMessage="Rekrutmen berhasil dipublikasikan.";
        shell.Go("detail",1);
        Assert.True(shell.CurrentPage!.HasInfo);
        shell.CurrentPage.InfoMessage="";shell.BackCommand.Execute(null);
        Assert.False(shell.CurrentPage!.HasInfo);
    }
    [Fact]public void SuccessFeedbackDoesNotCrossAccountSessions()
    {
        var session=new SessionState();session.SignIn(new(1,"Raka","a@example.test"));
        var shell=new ShellViewModel(session,(route,_)=>new Probe(session,route),(_,_)=>Task.FromResult(true));
        shell.Go("dashboard");shell.CurrentPage!.InfoMessage="Profil berhasil disimpan.";
        session.SignOut();session.SignIn(new(2,"Nadia","b@example.test"));shell.Go("dashboard");
        Assert.False(shell.CurrentPage!.HasInfo);
    }
    [Fact]public async Task LogoutCancellingConfirmationKeepsSessionAndPage()
    {
        var s=new SessionState();s.SignIn(new(1,"Raka","a@example.test"));
        var signedIn=s.Current;
        var shell=new ShellViewModel(s,(r,_)=>new Probe(s,r=="login"?"Masuk":r),(_,_)=>Task.FromResult(false));
        var old=new Probe(s,"Data pribadi");shell.Navigate(old);shell.IsMenuOpen=true;var revision=s.Revision;await shell.Logout();
        Assert.Same(signedIn,s.Current);
        Assert.Equal(revision,s.Revision);Assert.Same(old,shell.CurrentPage);Assert.True(old.IsActive);Assert.True(shell.IsMenuOpen);
    }
    [Fact]public async Task LogoutAfterConfirmationClearsCurrentPageAndIncrementsRevision()
    {
        var s=new SessionState();s.SignIn(new(1,"Raka","a@example.test"));
        var shell=new ShellViewModel(s,(r,_)=>new Probe(s,r=="login"?"Masuk":r),(_,_)=>Task.FromResult(true));
        var old=new Probe(s,"Data pribadi");shell.Navigate(old);old.InfoMessage="Profil berhasil disimpan.";shell.IsMenuOpen=true;var revision=s.Revision;await shell.Logout();
        Assert.Null(s.Current);Assert.True(s.Revision>revision);Assert.False(old.IsActive);Assert.False(shell.IsMenuOpen);Assert.Equal("Masuk",shell.CurrentPage?.Title);
        Assert.False(shell.CurrentPage!.HasInfo);
    }
}
