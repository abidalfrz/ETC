using Etc.Desktop.Features.Profiles;
namespace Etc.Desktop.Features.Applications;
public enum ApplicationStatus { Pending, Accepted, Rejected, Cancelled }
public sealed record ApplicationInput(int RecruitmentId,int PositionId,string Message,string? PortfolioUrl);
public sealed record MyApplicationRow(int Id,int RecruitmentId,string RecruitmentTitle,string PositionName,DateTimeOffset SubmittedAtUtc,ApplicationStatus Status)
{
    public string SubmittedText=>"Diajukan: "+SubmittedAtUtc.ToOffset(TimeSpan.FromHours(7)).ToString("dd/MM/yyyy, HH.mm 'WIB'");
    public string StatusText=>ApplicationText.Status(Status);
    public bool CanCancel=>Status==ApplicationStatus.Pending;
    public bool IsAccepted=>Status==ApplicationStatus.Accepted;
    public bool IsRejected=>Status==ApplicationStatus.Rejected;
}
public sealed record ApplicantRow(int Id,PublicProfile Applicant,int PositionId,string PositionName,string Message,string? PortfolioUrl,ApplicationStatus Status,DateTimeOffset SubmittedAtUtc,DateTimeOffset? DecidedAtUtc)
{
    public bool HasPortfolio=>!string.IsNullOrWhiteSpace(PortfolioUrl);
    public string StatusText=>ApplicationText.Status(Status);
    public bool CanDecide=>Status==ApplicationStatus.Pending;
    public bool IsAccepted=>Status==ApplicationStatus.Accepted;
    public bool IsRejected=>Status==ApplicationStatus.Rejected;
}
public static class ApplicationText
{
    public static string Status(ApplicationStatus status)=>status switch{ApplicationStatus.Pending=>"Menunggu",ApplicationStatus.Accepted=>"Diterima",ApplicationStatus.Rejected=>"Ditolak",_=>"Dibatalkan"};
}
