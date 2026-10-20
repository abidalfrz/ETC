using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Features.Bookmarks;
using Etc.Desktop.Features.Applications;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Recruitments;
public sealed partial class RecruitmentDetailViewModel(IRecruitmentService service,IProfileService profiles,IBookmarkService bookmarks,IApplicationService applications,SessionState session,int recruitmentId,Action<string,int> navigate,ExternalLinkService links):PageViewModel(session,"Detail rekrutmen")
{
    [ObservableProperty,NotifyPropertyChangedFor(nameof(HasDetail))]private RecruitmentDetail? detail;
    public bool HasDetail=>Detail is not null;
    public string StatusText=>Detail?.Status switch{RecruitmentStatus.Draft=>"Draft",RecruitmentStatus.Open=>"Dibuka",_=>"Ditutup"};
    public string CategoryText=>Detail?.Data.Category==RecruitmentCategory.Competition?"Lomba":"Proyek";
    [ObservableProperty]private PositionDetail? selectedPosition;
    [ObservableProperty]private string message="";
    [ObservableProperty]private string portfolioUrl="";
    [ObservableProperty]private bool isBookmarked;
    [ObservableProperty]private bool hasApplied;
    [ObservableProperty]private bool profileComplete;
    public bool IsOwner=>Detail?.OwnerId==Session.Current?.Id;
    public bool CanApply=>!IsOwner&&!HasApplied&&ProfileComplete&&Detail?.IsAvailable==true&&SelectedPosition?.HasSpace==true;
    public bool HasInfoUrl=>!string.IsNullOrWhiteSpace(Detail?.Data.InfoUrl);
    public string BookmarkLabel=>IsBookmarked?"Hapus bookmark":"Simpan bookmark";
    public string PublishedText=>Detail?.PublishedAtUtc is{} date?"Dipublikasikan: "+Wib(date):"Belum dipublikasikan";
    public string UpdatedText=>Detail is{} detail?"Diperbarui: "+Wib(detail.UpdatedAtUtc):"";
    private static string Wib(DateTimeOffset value)=>value.ToOffset(TimeSpan.FromHours(7)).ToString("d MMM yyyy, HH.mm 'WIB'",System.Globalization.CultureInfo.GetCultureInfo("id-ID"));
    public string DeadlineText=>Detail?.Data.DeadlineUtc?.ToOffset(TimeSpan.FromHours(7)).ToString("d MMM yyyy, HH.mm 'WIB'",System.Globalization.CultureInfo.GetCultureInfo("id-ID"))??"Belum diisi";
    public string ScheduleText=>Detail?.Data.StartsOn is DateOnly start?$"{start:dd/MM/yyyy} – {Detail.Data.EndsOn?.ToString("dd/MM/yyyy")??"Belum diketahui"}":"Jadwal kegiatan belum diketahui";
    public string FeeText=>Detail?.Data.FeeKind switch{FeeKind.Free=>"Gratis",FeeKind.Paid=>"Rp"+Detail.Data.FeeAmount?.ToString("N2",System.Globalization.CultureInfo.GetCultureInfo("id-ID"))+" · "+Detail.Data.FeeNote,_=>"Biaya belum diketahui"};
    public string DisabledReason=>IsOwner?"Ini rekrutmen Anda. Kelola pelamar melalui Rekrutmen saya.":HasApplied?"Anda sudah pernah melamar rekrutmen ini. Lihat hasil di Lamaran saya.":!ProfileComplete?"Lengkapi nama, angkatan, dan skill sebelum melamar.":Detail?.IsAvailable!=true?"Pendaftaran tidak tersedia: rekrutmen ditutup, tenggat berakhir, atau posisi penuh.":SelectedPosition?.HasSpace!=true?"Pilih posisi yang masih tersedia.":"";
    public override Task LoadAsync(CancellationToken ct=default)=>ReadAsync(async token=>(detail:await service.GetAsync(recruitmentId,token),complete:await profiles.IsMineCompleteAsync(token),applied:await applications.HasAppliedAsync(recruitmentId,token),saved:await bookmarks.IsSavedAsync(recruitmentId,token)),x=>
    {
        var selected=SelectedPosition?.Id;Detail=x.detail;ProfileComplete=x.complete;HasApplied=x.applied;IsBookmarked=x.saved;SelectedPosition=Detail.Positions.FirstOrDefault(p=>p.Id==selected)??Detail.Positions.FirstOrDefault(p=>p.HasSpace)??Detail.Positions.FirstOrDefault();Notify();
    },ct);
    private void Notify(){foreach(var name in new[]{nameof(IsOwner),nameof(CanApply),nameof(HasInfoUrl),nameof(BookmarkLabel),nameof(DeadlineText),nameof(ScheduleText),nameof(FeeText),nameof(DisabledReason),nameof(StatusText),nameof(CategoryText),nameof(PublishedText),nameof(UpdatedText)})OnPropertyChanged(name);}
    partial void OnSelectedPositionChanged(PositionDetail? value)=>Notify();
    [RelayCommand]private void OwnerProfile(){if(Detail is not null)navigate("profile-public",Detail.OwnerId);}
    [RelayCommand]private void Edit()=>navigate("recruitment-form",recruitmentId);
    [RelayCommand]private void Applicants()=>navigate("applicants",recruitmentId);
    [RelayCommand]private void CompleteProfile()=>navigate("profile",0);
    [RelayCommand]private void MyApplications()=>navigate("applications",0);
    [RelayCommand]private Task BookmarkAsync()=>RunAsync(async ct=>
    {
        var saved=!IsBookmarked;await bookmarks.SetAsync(recruitmentId,saved,ct);
        Apply(ct,()=>{IsBookmarked=saved;Notify();InfoMessage=saved?"Rekrutmen berhasil disimpan ke bookmark.":"Rekrutmen berhasil dihapus dari bookmark.";});
    });
    [RelayCommand]private Task OpenInfoAsync()=>RunAsync(ct=>links.OpenAsync(ExternalLinkService.Validate(Detail?.Data.InfoUrl)??throw new DomainException("Validation","Tautan belum tersedia."),ct));
    [RelayCommand]public Task SubmitAsync()=>RunAsync(async ct=>
    {
        if(!CanApply||SelectedPosition is null)throw new DomainException("Unavailable",DisabledReason);
        await applications.SubmitAsync(new(recruitmentId,SelectedPosition.Id,Message,PortfolioUrl),ct);
        Apply(ct,()=>{Message="";InfoMessage="Lamaran berhasil dikirim. Hasil seleksi tersedia di Lamaran saya.";});await LoadAsync(ct);
    });
}
