namespace Etc.Desktop.Features.Profiles;
public sealed record SkillOption(int Id,string Name);
public sealed record ProfileInput(string FullName,int Cohort,string Bio,string Experience,string? PortfolioUrl,string? LinkedInUrl,IReadOnlyList<int> SkillIds,byte[]? Photo);
public sealed record PublicProfile(int StudentId,string FullName,int? Cohort,string Bio,string Experience,string? PortfolioUrl,string? LinkedInUrl,IReadOnlyList<SkillOption> Skills,byte[]? Photo,bool IsDemo)
{
    public string Initials => string.Concat(FullName.Split(' ',StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x=>x[0])).ToUpperInvariant();
    public string SkillText => string.Join(" · ",Skills.Select(x=>x.Name));
}
