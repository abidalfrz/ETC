using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Etc.Desktop.Shared;
public sealed partial class NavigationItem(string label,string route,string iconKey):ObservableObject
{
    public string Label{get;}=label;public string Route{get;}=route;public string IconKey{get;}=iconKey;
    [ObservableProperty]private bool active;
}
public sealed partial class ShellViewModel:ObservableObject
{
    private readonly SessionState session;
    private readonly Func<string,int,PageViewModel> factory;
    private readonly Func<string,string,Task<bool>> confirmAsync;
    private readonly Stack<PageViewModel> history=new();
    public ShellViewModel(SessionState session,Func<string,int,PageViewModel> factory,Func<string,string,Task<bool>> confirmAsync,bool isDemoEnvironment=false)
    {
        this.session=session;this.factory=factory;this.confirmAsync=confirmAsync;IsDemoEnvironment=isDemoEnvironment;
        session.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(SessionState.Current)){OnPropertyChanged(nameof(IsAuthenticated));OnPropertyChanged(nameof(AccountName));OnPropertyChanged(nameof(ShowMenuToggle));}};
    }
    [ObservableProperty]private PageViewModel? currentPage;
    [ObservableProperty]private bool isCompact;
    [ObservableProperty]private bool isMenuOpen;
    public bool IsAuthenticated=>session.Current is not null;
    public bool IsDemoEnvironment{get;}
    public bool AnimationsEnabled=>MotionSettings.Enabled;
    public string AccountName=>session.Current?.FullName??"Mahasiswa";
    public bool CanGoBack=>history.Count>0;
    public bool IsNavVisible=>IsAuthenticated&&(!IsCompact||IsMenuOpen);
    public bool ShowMenuToggle=>IsAuthenticated&&IsCompact;
    public Avalonia.Controls.GridLength NavWidth=>new(IsAuthenticated&&!IsCompact?230:0);
    public Avalonia.Thickness CanvasMargin=>new(IsCompact?24:29);
    public double TopbarHeight=>IsCompact?56:64;
    public IReadOnlyList<NavigationItem> Navigation {get;}=[new("Dashboard","dashboard","home"),new("Cari rekrutmen","search","search"),new("Rekrutmen saya","mine","team"),new("Lamaran saya","applications","document"),new("Bookmark","bookmarks","bookmark"),new("Mentor","mentors","support")];
    public void Navigate(PageViewModel page)
    {
        var message=CurrentPage is{IsCurrent:true} current?current.InfoMessage:"";
        CurrentPage?.Deactivate();CurrentPage=page;page.Activate();page.InfoMessage=message;
        OnPropertyChanged(nameof(CanGoBack));_ = page.LoadAsync();
    }
    [RelayCommand]public async Task Logout(){if(!await confirmAsync("Keluar dari akun?","Anda perlu masuk kembali untuk menggunakan akun ini."))return;IsMenuOpen=false;CurrentPage?.Deactivate();foreach(var p in history)p.Deactivate();history.Clear();session.SignOut();Navigate(factory("login",0));OnPropertyChanged(nameof(NavWidth));OnPropertyChanged(nameof(IsNavVisible));}
    public void Go(string route,int id=0)
    {
        if(!IsAuthenticated&&route is not("login" or "register" or "onboarding"))route="login";
        if(route is "detail" or "applicants" or "recruitment-form" or "profile-public" or "mentor-detail")
        {if(CurrentPage is not null)history.Push(CurrentPage);}
        else{history.Clear();foreach(var item in Navigation)item.Active=item.Route==route;}
        IsMenuOpen=false;Navigate(factory(route,id));OnPropertyChanged(nameof(NavWidth));OnPropertyChanged(nameof(IsNavVisible));
    }
    [RelayCommand]private void NavigateTo(string route)=>Go(route);
    [RelayCommand]private void Profile()=>Go("profile",session.RequireStudentId());
    [RelayCommand]private void Menu()=>IsMenuOpen=!IsMenuOpen;
    [RelayCommand]private void Back(){if(history.TryPop(out var page))Navigate(page);}
    partial void OnIsCompactChanged(bool value){OnPropertyChanged(nameof(NavWidth));OnPropertyChanged(nameof(IsNavVisible));OnPropertyChanged(nameof(ShowMenuToggle));OnPropertyChanged(nameof(CanvasMargin));OnPropertyChanged(nameof(TopbarHeight));}
    partial void OnIsMenuOpenChanged(bool value)=>OnPropertyChanged(nameof(IsNavVisible));
}
