using Etc.Desktop.Features.Accounts;
using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class LoginViewModelTests
{
    private sealed class WaitingAccounts:IAccountService
    {
        public int Calls;public TaskCompletionSource<StudentIdentity> Result=new();
        public Task<StudentIdentity> LoginAsync(string email,string password,CancellationToken ct=default){Calls++;return Result.Task;}
        public Task<StudentIdentity> RegisterAsync(string name,string email,string password,CancellationToken ct=default)=>throw new NotSupportedException();
    }
    [Fact]public async Task LoginCannotSubmitTwiceAndLateResultCannotNavigateAfterDeactivation()
    {
        var accounts=new WaitingAccounts();var session=new SessionState();int navigations=0;
        var vm=new LoginViewModel(accounts,session,(_,_)=>navigations++){Email="raka@example.test",Password="test-password"};
        var first=vm.LoginAsync();await vm.LoginAsync();Assert.Equal(1,accounts.Calls);vm.Deactivate();session.SignOut();accounts.Result.SetResult(new(1,"Raka","raka@example.test"));await first;
        Assert.Equal(0,navigations);Assert.Equal("",vm.Password);Assert.Null(session.Current);
    }
}
