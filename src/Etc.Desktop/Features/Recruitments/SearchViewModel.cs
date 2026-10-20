using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Bookmarks;
namespace Etc.Desktop.Features.Recruitments;
public sealed partial class SearchViewModel(IRecruitmentQueries queries,IProfileService profiles,IBookmarkService bookmarks,SessionState session,Action<string,int> navigate):PageViewModel(session,"Cari rekrutmen")
{
    [ObservableProperty]private IReadOnlyList<RecruitmentSummary> items=[];
    [ObservableProperty]private string searchText="";
    [ObservableProperty]private int pageNumber=1;
    [ObservableProperty]private int total;
    [ObservableProperty]private string selectedCategory="Semua";
    [ObservableProperty]private SkillOption? selectedSkill;
    [ObservableProperty]private string positionText="";
    [ObservableProperty]private bool onlyAvailable=true;
    [ObservableProperty]private IReadOnlyList<SkillOption> skills=[new(0,"Semua skill")];
    public IReadOnlyList<string> Categories {get;}=["Semua","Lomba","Proyek"];
    public int Pages=>Math.Max(1,(Total+11)/12);
    public string PagingText=>$"Halaman {PageNumber} dari {Pages} · {Total} rekrutmen";
    public bool HasItems=>Items.Count>0;
    public override Task LoadAsync(CancellationToken ct=default)
    {
        var f=new RecruitmentFilter(SearchText,SelectedCategory=="Lomba"?RecruitmentCategory.Competition:SelectedCategory=="Proyek"?RecruitmentCategory.Project:null,SelectedSkill?.Id>0?SelectedSkill.Id:null,PositionText,OnlyAvailable,PageNumber);
        return ReadAsync(async token=>
        {
            var rows=await queries.SearchAsync(f,token);
            var catalog=profiles is null?Array.Empty<SkillOption>():await profiles.ListSkillsAsync(token);
            return(rows,catalog);
        },result=>{Items=result.rows.Items;Total=result.rows.Total;PageNumber=result.rows.PageNumber;if(Skills.Count<=1)Skills=[new(0,"Semua skill"),..result.catalog];Notify();},ct);
    }
    private void Notify(){OnPropertyChanged(nameof(Pages));OnPropertyChanged(nameof(PagingText));OnPropertyChanged(nameof(HasItems));}
    partial void OnSearchTextChanged(string value)=>PageNumber=1;
    partial void OnSelectedCategoryChanged(string value)=>PageNumber=1;
    partial void OnSelectedSkillChanged(SkillOption? value)=>PageNumber=1;
    partial void OnPositionTextChanged(string value)=>PageNumber=1;
    partial void OnOnlyAvailableChanged(bool value)=>PageNumber=1;
    [RelayCommand(AllowConcurrentExecutions=true)]private async Task SearchAsync(){PageNumber=1;await LoadAsync();}
    [RelayCommand]private async Task ResetAsync(){SearchText="";SelectedCategory="Semua";SelectedSkill=null;PositionText="";OnlyAvailable=true;await SearchAsync();}
    [RelayCommand]private async Task NextAsync(){if(PageNumber<Pages){PageNumber++;await LoadAsync();}}
    [RelayCommand]private async Task PreviousAsync(){if(PageNumber>1){PageNumber--;await LoadAsync();}}
    [RelayCommand]private void Open(int id)=>navigate("detail",id);
    [RelayCommand]private Task BookmarkAsync(RecruitmentSummary row)=>RunAsync(async ct=>
    {
        var saved=!row.IsBookmarked;await bookmarks.SetAsync(row.Id,saved,ct);
        Apply(ct,()=>InfoMessage=saved?"Rekrutmen berhasil disimpan ke bookmark.":"Rekrutmen berhasil dihapus dari bookmark.");
        await LoadAsync(ct);
    });
    public override void Deactivate(){base.Deactivate();Items=[];Notify();}
}
