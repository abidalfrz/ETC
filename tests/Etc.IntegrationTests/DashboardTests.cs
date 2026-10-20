using Etc.Desktop.Features.Dashboard;
using Etc.Desktop.Features.Applications;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class DashboardTests(DatabaseFixture db)
{
    [Fact]public async Task StatisticsMatchOutlineDefinitionsAndCurrentAccount()
    {
        var t=new Scenario(db);await t.InitializeAsync();await t.OpenAsync();var expired=await t.OpenAsync();
        await db.ExecuteAsync($"UPDATE Recruitments SET DeadlineUtc='2020-01-01' WHERE Id={expired}");
        var closed=await t.OpenAsync();await t.Recruitments.CloseAsync(closed);
        var result=await new DashboardService(db.Factory,t.Session,t.Clock).GetMineAsync();
        Assert.Equal(new DashboardStats(1,0,0,0),result.Stats);
        t.Session.SignIn(t.B);result=await new DashboardService(db.Factory,t.Session,t.Clock).GetMineAsync();Assert.Equal(new DashboardStats(0,0,0,0),result.Stats);
    }
}
