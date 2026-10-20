using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Mentors;
public sealed partial class MentorsViewModel(IMentorService service,SessionState session,Action<string,int> navigate):PagedPageViewModel<MentorSummary>(session,"Mentor")
{
    [ObservableProperty]private string searchText="";
    public bool HasSearch=>!string.IsNullOrWhiteSpace(SearchText);
    partial void OnSearchTextChanged(string value)=>OnPropertyChanged(nameof(HasSearch));
    protected override Task<Page<MentorSummary>> FetchPageAsync(int page,CancellationToken ct)=>service.SearchAsync(SearchText,page,ct);
    [RelayCommand]private async Task SearchAsync(){PageNumber=1;await LoadAsync();}
    [RelayCommand]private async Task ResetAsync(){SearchText="";await SearchAsync();}
    [RelayCommand]private async Task FilterSkillAsync(string skill){SearchText=skill;await SearchAsync();}
    [RelayCommand]private void Open(int id)=>navigate("mentor-detail",id);
}
