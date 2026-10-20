using Etc.Desktop.Features.Profiles;
namespace Etc.Desktop.Features.Recruitments;
public enum RecruitmentStatus { Draft, Open, Closed }
public enum RecruitmentCategory { Competition, Project }
public enum ActivityMode { Online, Offline, Hybrid }
public enum FeeKind { Unknown, Free, Paid }
public sealed record PositionInput(int? Id,string Name,int Capacity,IReadOnlyList<int> SkillIds);
public sealed record RecruitmentInput(string Title,RecruitmentCategory Category,string ActivityName,string Description,DateTimeOffset? DeadlineUtc,DateOnly? StartsOn,DateOnly? EndsOn,ActivityMode? Mode,string? Location,FeeKind FeeKind,decimal? FeeAmount,string? FeeNote,string? InfoUrl,string? ContactName,string? ContactChannel,IReadOnlyList<PositionInput> Positions);
public sealed record PositionDetail(int Id,string Name,int Capacity,int AcceptedCount,IReadOnlyList<SkillOption> Skills)
{
    public string SkillText=>string.Join(" · ",Skills.Select(x=>x.Name));
    public string QuotaText=>$"{AcceptedCount} dari {Capacity} anggota diterima · {Capacity-AcceptedCount} tempat tersisa";
    public bool HasSpace=>AcceptedCount<Capacity;
}
public sealed record RecruitmentSummary(int Id,string Title,RecruitmentCategory Category,DateTimeOffset? PublishedAtUtc,DateTimeOffset? DeadlineUtc,RecruitmentStatus Status,bool IsAvailable,bool IsBookmarked,IReadOnlyList<string> PositionNames)
{
    public IReadOnlyList<PositionDetail> Positions{get;init;}=[];
    public string CategoryText=>Category==RecruitmentCategory.Competition?"Lomba":"Proyek";
    public string StatusText=>Status==RecruitmentStatus.Draft?"Draft":Status==RecruitmentStatus.Closed?"Ditutup":IsAvailable?"Dibuka":"Pendaftaran tidak tersedia";
    public string PositionsText=>Positions.Count>0?string.Join("\n\n",Positions.Select(p=>$"{p.Name} · {p.QuotaText}\nSkill: {p.SkillText}")):PositionNames.Count>0?string.Join(" · ",PositionNames):"Belum ada posisi";
    public bool IsDraft=>Status==RecruitmentStatus.Draft;
    public bool IsOpen=>Status==RecruitmentStatus.Open;
    public string DeadlineText=>DeadlineUtc?.ToOffset(TimeSpan.FromHours(7)).ToString("d MMM yyyy, HH.mm 'WIB'",System.Globalization.CultureInfo.GetCultureInfo("id-ID"))??"Tenggat belum diisi";
}
public sealed record RecruitmentDetail(int Id,int OwnerId,PublicProfile Owner,RecruitmentInput Data,RecruitmentStatus Status,DateTimeOffset? PublishedAtUtc,DateTimeOffset UpdatedAtUtc,bool IsAvailable,IReadOnlyList<PositionDetail> Positions,bool HasApplications=false);
