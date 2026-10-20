using Etc.Desktop.Features.Applications;
using Etc.Desktop.Shared;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class AcceptanceConcurrencyTests(DatabaseFixture db)
{
    [Fact]public async Task AcceptanceAndCancellationCannotBothSucceed()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0].Id;
        t.Session.SignIn(t.B);var svc=new ApplicationService(db.Factory,t.Session,t.Clock);var app=await svc.SubmitAsync(new(id,pos,"Pesan",null));
        var owner=new SessionState();owner.SignIn(t.A);var ownerService=new ApplicationService(db.Factory,owner,t.Clock);
        async Task<bool> Attempt(Func<Task> action){try{await action();return true;}catch(DomainException e)when(e.Code=="Conflict"){return false;}}
        var outcomes=await Task.WhenAll(Attempt(()=>svc.CancelAsync(app)),Attempt(()=>ownerService.DecideAsync(app,ApplicationStatus.Accepted)));
        Assert.Single(outcomes,x=>x);Assert.Contains((await svc.ListMineAsync(1)).Items.Single().Status,new[]{ApplicationStatus.Accepted,ApplicationStatus.Cancelled});
    }
    [Fact]public async Task TwoAcceptancesForLastSeatProduceExactlyOneSuccess()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0].Id;
        var svc=new ApplicationService(db.Factory,t.Session,t.Clock);t.Session.SignIn(t.B);var b=await svc.SubmitAsync(new(id,pos,"Nadia",null));
        t.Session.SignIn(t.C);var c=await svc.SubmitAsync(new(id,pos,"Dimas",null));t.Session.SignIn(t.A);
        async Task<string> Accept(int app){try{await new ApplicationService(db.Factory,t.Session,t.Clock).DecideAsync(app,ApplicationStatus.Accepted);return "ok";}catch(DomainException e){return e.Code;}}
        var outcomes=await Task.WhenAll(Accept(b),Accept(c));Assert.Single(outcomes.Where(x=>x=="ok"));Assert.Single(outcomes.Where(x=>x=="CapacityFull"));
        Assert.Equal(1,Convert.ToInt32(await db.ScalarAsync($"SELECT COUNT(*) FROM Applications WHERE PositionId={pos} AND Status=1")));
    }
}
