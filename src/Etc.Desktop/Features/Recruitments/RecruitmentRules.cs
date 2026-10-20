namespace Etc.Desktop.Features.Recruitments;
public static class RecruitmentRules
{
    public static bool CanApply(RecruitmentStatus status,DateTimeOffset? deadlineUtc,int accepted,int capacity,DateTimeOffset nowUtc)=>status==RecruitmentStatus.Open&&deadlineUtc.HasValue&&nowUtc<deadlineUtc.Value&&accepted<capacity;
}
