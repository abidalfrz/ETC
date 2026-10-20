using Etc.Desktop.Infrastructure;
using Etc.Desktop.Shared;
using Etc.Desktop.Features.Applications;
using Etc.Desktop.Features.Recruitments;
namespace Etc.Desktop.Features.Dashboard;
public interface IDashboardService {Task<DashboardData> GetMineAsync(CancellationToken ct=default);}
public sealed class DashboardService(SqlConnectionFactory connections,SessionState session,TimeProvider clock):IDashboardService
{
    public async Task<DashboardData> GetMineAsync(CancellationToken ct=default)
    {
        var actor=session.RequireStudentId();await using var c=await connections.OpenAsync(ct);
        await using var cmd=Db.Command(c,null,"SELECT (SELECT COUNT(*) FROM Recruitments WHERE OwnerId=@actor AND Status=1 AND DeadlineUtc>@now) AS ActiveRecruitments,(SELECT COUNT(*) FROM Applications WHERE StudentId=@actor AND Status=0) AS PendingApplications,(SELECT COUNT(*) FROM Applications WHERE StudentId=@actor AND Status=1) AS AcceptedApplications,(SELECT COUNT(*) FROM Bookmarks WHERE StudentId=@actor) AS Bookmarks FOR JSON PATH,WITHOUT_ARRAY_WRAPPER",("actor",actor),("now",clock.GetUtcNow()));
        var stats=(await Db.JsonAsync<DashboardStats>(cmd,ct))!;
        var mine=await new RecruitmentService(connections,session,clock).ListMineAsync(1,ct);
        var apps=await new ApplicationService(connections,session,clock).ListMineAsync(1,ct);
        return new(stats,mine.Items.Take(5).ToArray(),apps.Items.Take(5).ToArray());
    }
}
