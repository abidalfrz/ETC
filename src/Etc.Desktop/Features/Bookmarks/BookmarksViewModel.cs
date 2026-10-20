using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Bookmarks;
public sealed partial class BookmarksViewModel(IBookmarkService service,SessionState session,Action<string,int> navigate):PagedPageViewModel<RecruitmentSummary>(session,"Bookmark")
{
    protected override Task<Page<RecruitmentSummary>> FetchPageAsync(int page,CancellationToken ct)=>service.ListMineAsync(page,ct);
    [RelayCommand]private void Open(int id)=>navigate("detail",id);
    [RelayCommand]private void Search()=>navigate("search",0);
    [RelayCommand]private Task RemoveAsync(int id)=>RunAsync(async ct=>{await service.SetAsync(id,false,ct);Apply(ct,()=>InfoMessage="Rekrutmen berhasil dihapus dari bookmark.");await LoadAsync(ct);});
}
