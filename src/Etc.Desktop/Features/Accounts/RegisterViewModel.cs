using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Accounts;
public sealed partial class RegisterViewModel(IAccountService accounts,SessionState session,Action<string,int> navigate):PageViewModel(session,"Buat akun")
{
    [ObservableProperty]private string fullName="";
    [ObservableProperty]private string email="";
    [ObservableProperty]private string password="";
    [RelayCommand]private Task RegisterAsync()=>RunAsync(async ct=>{await accounts.RegisterAsync(FullName,Email,Password,ct);Apply(ct,()=>{Password="";InfoMessage="Akun berhasil dibuat. Silakan masuk.";});});
    [RelayCommand]private void Login()=>navigate("login",0);
    public override void Deactivate(){Password="";base.Deactivate();}
}
