using Etc.Desktop.Features.Recruitments;
namespace Etc.UnitTests;
public sealed class DeadlineTests
{
    [Fact]public void DeadlineIsClosedAtExactInstant()
    {
        var d=new DateTimeOffset(2026,11,1,23,59,0,TimeSpan.FromHours(7));
        Assert.True(RecruitmentRules.CanApply(RecruitmentStatus.Open,d,0,1,d.AddTicks(-1)));
        Assert.False(RecruitmentRules.CanApply(RecruitmentStatus.Open,d,0,1,d));
        Assert.False(RecruitmentRules.CanApply(RecruitmentStatus.Open,d,1,1,d.AddMinutes(-1)));
        Assert.False(RecruitmentRules.CanApply(RecruitmentStatus.Closed,d,0,1,d.AddMinutes(-1)));
        Assert.False(RecruitmentRules.CanApply(RecruitmentStatus.Open,d.ToUniversalTime(),0,1,d));
    }
}
