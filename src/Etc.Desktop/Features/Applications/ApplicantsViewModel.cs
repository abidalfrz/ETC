using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Features.Recruitments;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Applications;
public sealed partial class ApplicantsViewModel(IApplicationService service,IRecruitmentService recruitments,SessionState session,int recruitmentId,Action<string,int> navigate,Func<string,string,Task<bool>> confirm,ExternalLinkService links):PagedPageViewModel<ApplicantRow>(session,"Pelamar rekrutmen")
{
    [ObservableProperty]private ApplicantRow? selectedApplicant;
    [ObservableProperty]private string recruitmentTitle="";
    [ObservableProperty]private string quotaText="";
    [ObservableProperty]private IReadOnlyList<PositionFilter> positions=[];
    [ObservableProperty]private PositionFilter? selectedPosition;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(ShowList)),NotifyPropertyChangedFor(nameof(ShowDetail))]private bool isCompact;
    [ObservableProperty,NotifyPropertyChangedFor(nameof(ShowList)),NotifyPropertyChangedFor(nameof(ShowDetail))]private bool isReviewingApplicant;
    public Avalonia.Vector ListScrollOffset{get;private set;}
    public bool ShowList=>!IsCompact||!IsReviewingApplicant;
    public bool ShowDetail=>HasSelection&&(!IsCompact||IsReviewingApplicant);
    partial void OnSelectedPositionChanged(PositionFilter? value){PageNumber=1;}
    public bool HasSelection=>SelectedApplicant is not null;
    public bool CanDecide=>SelectedApplicant?.CanDecide==true&&!IsBusy;
    protected override Task<Page<ApplicantRow>> FetchPageAsync(int page,CancellationToken ct)=>SelectedPosition is{Id:>0} p?service.ListPositionApplicantsAsync(recruitmentId,p.Id,page,ct):service.ListApplicantsAsync(recruitmentId,page,ct);
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(async token=>(rows:await FetchPageAsync(PageNumber,token),detail:await recruitments.GetAsync(recruitmentId,token)),x=>
    {
        var selected=SelectedApplicant?.Id;SetPage(x.rows);RecruitmentTitle=x.detail.Data.Title;QuotaText=string.Join("\n",x.detail.Positions.Select(p=>p.Name+": "+p.QuotaText));
        if(Positions.Count==0){Positions=[new(0,"Semua posisi"),..x.detail.Positions.Select(p=>new PositionFilter(p.Id,p.Name))];SelectedPosition=Positions[0];}
        SelectedApplicant=Items.FirstOrDefault(a=>a.Id==selected)??Items.FirstOrDefault();OnPropertyChanged(nameof(CanDecide));
    },ct);
    partial void OnSelectedApplicantChanged(ApplicantRow? value){OnPropertyChanged(nameof(HasSelection));OnPropertyChanged(nameof(CanDecide));OnPropertyChanged(nameof(ShowDetail));}
    [RelayCommand]private void Review(ApplicantRow row){ListScrollOffset=ScrollOffset;SelectedApplicant=row;IsReviewingApplicant=true;if(IsCompact)ScrollOffset=default;}
    [RelayCommand]private void BackToList(){IsReviewingApplicant=false;ScrollOffset=ListScrollOffset;}
    [RelayCommand]private void Profile(){if(SelectedApplicant is not null)navigate("profile-public",SelectedApplicant.Applicant.StudentId);}
    [RelayCommand]private Task PortfolioAsync()=>RunAsync(ct=>links.OpenAsync(ExternalLinkService.Validate(SelectedApplicant?.PortfolioUrl)??throw new DomainException("Validation","Tautan portofolio belum tersedia."),ct));
    [RelayCommand]private Task AcceptAsync()=>DecideAsync(ApplicationStatus.Accepted);
    [RelayCommand]private Task RejectAsync()=>DecideAsync(ApplicationStatus.Rejected);
    private Task DecideAsync(ApplicationStatus status)=>RunAsync(async ct=>
    {
        var row=SelectedApplicant;if(row is null)return;
        if(await confirm(status==ApplicationStatus.Accepted?"Terima pelamar?":"Tolak pelamar?",$"{row.Applicant.FullName}\n{row.PositionName}\nKeputusan ini tidak dapat diubah pada versi demonstrasi."))
        {await service.DecideAsync(row.Id,status,ct);Apply(ct,()=>InfoMessage=status==ApplicationStatus.Accepted?"Pelamar berhasil diterima.":"Pelamar berhasil ditolak.");await LoadAsync(ct);}
    });
    public override void Deactivate(){base.Deactivate();SelectedApplicant=null;}
}
public sealed record PositionFilter(int Id,string Name);
