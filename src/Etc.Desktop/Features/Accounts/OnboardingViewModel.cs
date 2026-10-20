using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Accounts;
public sealed partial class OnboardingViewModel(SessionState session,Action<string,int> navigate):PageViewModel(session,"Temukan tim. Mulai kolaborasi.")
{
    [RelayCommand]private void Login()=>navigate("login",0);
    [RelayCommand]private void Register()=>navigate("register",0);
}
