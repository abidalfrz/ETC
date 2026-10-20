using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Accounts;
public sealed partial class LoginViewModel(IAccountService accounts,SessionState session,Action<string,int> navigate):PageViewModel(session,"Masuk")
{
    [ObservableProperty]private string email="";
    [ObservableProperty]private string password="";
    [RelayCommand]public Task LoginAsync()=>RunAsync(async ct=>
    {
        var identity=await accounts.LoginAsync(Email,Password,ct);
        if(IsActive&&Session.Current?.Id==identity.Id){Password="";navigate("dashboard",0);}
    });
    [RelayCommand]private void Register()=>navigate("register",0);
    public override void Deactivate(){Password="";base.Deactivate();}
}
