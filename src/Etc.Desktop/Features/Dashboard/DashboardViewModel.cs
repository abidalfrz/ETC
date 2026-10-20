using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Dashboard;
public sealed partial class DashboardViewModel(IDashboardService service,SessionState session,Action<string,int> navigate):PageViewModel(session,"Dashboard")
{
    [ObservableProperty]private DashboardStats stats=new(0,0,0,0);
    [ObservableProperty]private IReadOnlyList<MyApplicationRow> recentApplications=[];
    [ObservableProperty]private IReadOnlyList<RecruitmentSummary> myRecentRecruitments=[];
    [ObservableProperty]private bool hasData;
    public bool HasApplications=>RecentApplications.Count>0;
    public bool HasRecruitments=>MyRecentRecruitments.Count>0;
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(token=>service.GetMineAsync(token),x=>{Stats=x.Stats;RecentApplications=x.RecentApplications;MyRecentRecruitments=x.MyRecentRecruitments;HasData=true;OnPropertyChanged(nameof(HasApplications));OnPropertyChanged(nameof(HasRecruitments));},ct);
    [RelayCommand]private void Search()=>navigate("search",0);
    [RelayCommand]private void Applications()=>navigate("applications",0);
    [RelayCommand]private void Recruitments()=>navigate("mine",0);
    [RelayCommand]private void Bookmarks()=>navigate("bookmarks",0);
    [RelayCommand]private void OpenRecruitment(int id)=>navigate("detail",id);
    [RelayCommand]private void OpenApplicants(int id)=>navigate("applicants",id);
    public override void Deactivate(){base.Deactivate();Stats=new(0,0,0,0);RecentApplications=[];MyRecentRecruitments=[];HasData=false;}
}
