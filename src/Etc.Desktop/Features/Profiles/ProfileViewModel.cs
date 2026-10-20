using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Profiles;
public sealed partial class ProfileViewModel(IProfileService profiles,SessionState session,int studentId,Func<Task<byte[]?>> pickPhoto,ExternalLinkService links):PageViewModel(session,"Profil mahasiswa")
{
    private readonly int id=studentId==0?session.RequireStudentId():studentId;
    public bool CanEdit=>id==Session.Current?.Id;
    [ObservableProperty]private string fullName="";
    [ObservableProperty]private string cohort="";
    [ObservableProperty]private string bio="";
    [ObservableProperty]private string experience="";
    [ObservableProperty]private string portfolioUrl="";
    [ObservableProperty]private string linkedInUrl="";
    [ObservableProperty]private byte[]? photo;
    [ObservableProperty]private bool isDemo;
    [ObservableProperty]private IReadOnlyList<SkillChoice> skills=[];
    [ObservableProperty]private string selectedSkillText="";
    public string Initials=>string.Concat(FullName.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0]));
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(async token=>(profile:await profiles.GetAsync(id,token),catalog:await profiles.ListSkillsAsync(token)),x=>
    {
        FullName=x.profile.FullName;Cohort=x.profile.Cohort?.ToString()??"";Bio=x.profile.Bio;Experience=x.profile.Experience;PortfolioUrl=x.profile.PortfolioUrl??"";LinkedInUrl=x.profile.LinkedInUrl??"";Photo=x.profile.Photo;IsDemo=x.profile.IsDemo;
        Skills=x.catalog.Select(s=>new SkillChoice(s.Id,s.Name,x.profile.Skills.Any(p=>p.Id==s.Id))).ToArray();SelectedSkillText=x.profile.SkillText;OnPropertyChanged(nameof(Initials));
    },ct);
    [RelayCommand]public Task SaveAsync()=>RunAsync(async ct=>
    {
        if(!CanEdit)throw new DomainException("Unauthorized","Profil ini hanya dapat dibaca.");
        if(!int.TryParse(Cohort,out var year))throw new DomainException("Validation","Angkatan harus berupa tahun empat digit.");
        await profiles.SaveMineAsync(new(FullName,year,Bio,Experience,PortfolioUrl,LinkedInUrl,Skills.Where(s=>s.IsSelected).Select(s=>s.Id).ToArray(),Photo),ct);
        Apply(ct,()=>InfoMessage="Profil berhasil disimpan.");
    });
    [RelayCommand]private Task ChoosePhotoAsync()=>RunAsync(async ct=>{if(!CanEdit)return;var bytes=await pickPhoto();if(bytes is not null){PhotoValidator.Validate(bytes);Apply(ct,()=>Photo=bytes);}});
    [RelayCommand]private void RemovePhoto(){if(CanEdit)Photo=null;}
    [RelayCommand]private Task OpenPortfolioAsync()=>OpenAsync(PortfolioUrl);
    [RelayCommand]private Task OpenLinkedInAsync()=>OpenAsync(LinkedInUrl);
    private Task OpenAsync(string url)=>RunAsync(ct=>links.OpenAsync(ExternalLinkService.Validate(url)??throw new DomainException("Validation","Tautan belum tersedia."),ct));
}
