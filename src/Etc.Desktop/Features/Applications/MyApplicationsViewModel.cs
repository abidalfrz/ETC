using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Applications;
public sealed partial class MyApplicationsViewModel(IApplicationService service,SessionState session,Action<string,int> navigate,Func<string,string,Task<bool>> confirm):PagedPageViewModel<MyApplicationRow>(session,"Lamaran saya")
{
    protected override Task<Page<MyApplicationRow>> FetchPageAsync(int page,CancellationToken ct)=>service.ListMineAsync(page,ct);
    [RelayCommand]private void Open(int recruitmentId)=>navigate("detail",recruitmentId);
    [RelayCommand]private void Search()=>navigate("search",0);
    [RelayCommand]private Task CancelAsync(MyApplicationRow row)=>RunAsync(async ct=>{if(await confirm("Batalkan lamaran?",$"{row.RecruitmentTitle}\nPosisi: {row.PositionName}")){await service.CancelAsync(row.Id,ct);Apply(ct,()=>InfoMessage="Lamaran berhasil dibatalkan.");await LoadAsync(ct);}});
}
