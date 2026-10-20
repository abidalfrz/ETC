using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class SessionTests
{
    [Fact]public void LateLoginCannotRestoreLoggedOutSession()
    {
        var s=new SessionState();var revision=s.Revision;s.SignOut();
        Assert.False(s.TrySignIn(new(1,"Raka","raka@example.test"),revision));Assert.Null(s.Current);
    }
    [Fact]public void UpdatingNameKeepsRevisionAndCannotUpdateAnotherAccount()
    {
        var s=new SessionState();s.SignIn(new(1,"Raka","raka@example.test"));var revision=s.Revision;
        s.UpdateName(1,"Raka baru");Assert.Equal("Raka baru",s.Current?.FullName);Assert.Equal(revision,s.Revision);
        s.SignOut();s.SignIn(new(2,"Nadia","nadia@example.test"));s.UpdateName(1,"Nama terlambat");Assert.Equal("Nadia",s.Current?.FullName);
    }
    [Fact] public void LogoutClearsIdentityAndChangesRevision()
    {
        var s = new SessionState();
        s.SignIn(new(1,"Raka","raka@example.test"));
        var previous = s.Revision;
        Assert.Equal(1,s.RequireStudentId());
        s.SignOut();
        Assert.Null(s.Current);
        Assert.True(s.Revision>previous);
        Assert.Throws<DomainException>(()=>s.RequireStudentId());
    }
}
