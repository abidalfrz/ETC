using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace Etc.Desktop.Shared;
public abstract partial class PagedPageViewModel<T>(SessionState session,string title):PageViewModel(session,title)
{
    [ObservableProperty]private IReadOnlyList<T> items=[];
    [ObservableProperty]private int pageNumber=1;
    [ObservableProperty]private int total;
    public int Pages=>Math.Max(1,(Total+11)/12);
    public string PagingText=>$"Halaman {PageNumber} dari {Pages} · {Total} hasil";
    public bool HasItems=>Items.Count>0;
    protected abstract Task<Page<T>> FetchPageAsync(int page,CancellationToken ct);
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(token=>FetchPageAsync(PageNumber,token),SetPage,ct);
    protected void SetPage(Page<T> page){Items=page.Items;PageNumber=page.PageNumber;Total=page.Total;OnPropertyChanged(nameof(Pages));OnPropertyChanged(nameof(PagingText));OnPropertyChanged(nameof(HasItems));}
    [RelayCommand]private async Task NextAsync(){if(PageNumber<Pages){PageNumber++;await LoadAsync();}}
    [RelayCommand]private async Task PreviousAsync(){if(PageNumber>1){PageNumber--;await LoadAsync();}}
    public override void Deactivate(){base.Deactivate();Items=[];OnPropertyChanged(nameof(HasItems));}
}
