using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Recruitments;
public sealed partial class MyRecruitmentsViewModel(IRecruitmentService service,SessionState session,Action<string,int> navigate,Func<string,string,Task<bool>> confirm):PagedPageViewModel<RecruitmentSummary>(session,"Rekrutmen saya")
{
    protected override Task<Page<RecruitmentSummary>> FetchPageAsync(int page,CancellationToken ct)=>service.ListMineAsync(page,ct);
    [RelayCommand]private void Create()=>navigate("recruitment-form",0);
    [RelayCommand]private void Open(int id)=>navigate("detail",id);
    [RelayCommand]private void Edit(int id)=>navigate("recruitment-form",id);
    [RelayCommand]private void Applicants(int id)=>navigate("applicants",id);
    [RelayCommand]private Task PublishAsync(RecruitmentSummary row)=>RunAsync(async ct=>{if(await confirm("Publikasikan rekrutmen?",row.Title)){await service.PublishAsync(row.Id,ct);Apply(ct,()=>InfoMessage="Rekrutmen berhasil dipublikasikan.");await LoadAsync(ct);}});
    [RelayCommand]private Task CloseAsync(RecruitmentSummary row)=>RunAsync(async ct=>{if(await confirm("Tutup rekrutmen?",$"{row.Title}\nLamaran baru tidak akan diterima.")){await service.CloseAsync(row.Id,ct);Apply(ct,()=>InfoMessage="Rekrutmen berhasil ditutup.");await LoadAsync(ct);}});
    [RelayCommand]private Task DeleteAsync(RecruitmentSummary row)=>RunAsync(async ct=>{if(await confirm("Hapus draft?",row.Title)){await service.DeleteDraftAsync(row.Id,ct);Apply(ct,()=>InfoMessage="Draft rekrutmen berhasil dihapus.");await LoadAsync(ct);}});
}
