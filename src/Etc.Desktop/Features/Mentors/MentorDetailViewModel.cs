using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Mentors;
public sealed partial class MentorDetailViewModel:PageViewModel
{
    private readonly IMentorService service;private readonly int mentorId;private readonly ExternalLinkService links;private readonly Uri? form;
    public MentorDetailViewModel(IMentorService service,SessionState session,int mentorId,ExternalLinkService links,string? formUrl):base(session,"Detail mentor")
    {
        this.service=service;this.mentorId=mentorId;this.links=links;
        try{form=ExternalLinkService.Validate(formUrl);}catch(DomainException){form=null;}
    }
    [ObservableProperty]private MentorDetail? detail;
    public bool CanRequestMentor=>form is not null;
    public string FormReason=>form is null?"Formulir mentoring belum tersedia.":"Pengajuan dilanjutkan melalui Google Form di browser.";
    public bool HasPortfolio=>PortfolioUri is not null;
    public bool HasAchievements=>!string.IsNullOrWhiteSpace(Detail?.Achievements);
    public string ExperienceText=>HasPortfolio?"Pengalaman dan karya mentor tersedia melalui portofolio.":string.IsNullOrWhiteSpace(Detail?.Experience)?"Pengalaman belum dicantumkan.":Detail.Experience;
    private Uri? PortfolioUri
    {
        get{try{return ExternalLinkService.Validate(Detail?.Experience);}catch(DomainException){return null;}}
    }
    partial void OnDetailChanged(MentorDetail? value){OnPropertyChanged(nameof(HasPortfolio));OnPropertyChanged(nameof(HasAchievements));OnPropertyChanged(nameof(ExperienceText));}
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(token=>service.GetAsync(mentorId,token),x=>Detail=x,ct);
    [RelayCommand]private Task RequestMentorAsync()=>RunAsync(ct=>links.OpenAsync(form??throw new DomainException("Unavailable","Formulir mentoring belum tersedia."),ct));
    [RelayCommand]private Task PortfolioAsync()=>RunAsync(ct=>links.OpenAsync(PortfolioUri??throw new DomainException("Unavailable","Portofolio belum tersedia."),ct));
}
