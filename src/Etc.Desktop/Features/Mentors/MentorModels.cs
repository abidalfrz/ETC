using Etc.Desktop.Features.Profiles;
namespace Etc.Desktop.Features.Mentors;
public sealed record MentorSummary(int Id,string Name,byte[]? Photo,IReadOnlyList<SkillOption> Skills,bool IsDemo)
{
    public int? Cohort{get;init;}
    public string? StudentNumber{get;init;}
    public string Focus{get;init;}="";
    public string Experience{get;init;}="";
    public string CohortText=>Cohort is{} year?$"Angkatan {year}":"Angkatan belum dicantumkan";
    public string FocusText=>Focus.Replace("Competition","Lomba",StringComparison.Ordinal).Replace("Project","Proyek",StringComparison.Ordinal);
    public string ExperiencePreview=>string.IsNullOrWhiteSpace(Experience)?"Pengalaman belum dicantumkan.":Uri.TryCreate(Experience,UriKind.Absolute,out var uri)&&uri.Scheme is "http" or "https"?"Portofolio tersedia di profil mentor.":Experience;
    public string SkillText=>string.Join(" · ",Skills.Select(x=>x.Name));
    public string Initials=>string.Concat(Name.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0]));
}
public sealed record MentorDetail(MentorSummary Summary,string Experience,string Achievements);
