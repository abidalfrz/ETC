using Etc.Desktop.Shared;
using Etc.Desktop.Features.Recruitments;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class RecruitmentTests(DatabaseFixture db)
{
    [Fact]public async Task DraftIsPrivateAndOnlyOwnerCanMutate()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.Recruitments.SaveDraftAsync(null,t.Input());
        t.Session.SignIn(t.B);
        Assert.Equal("NotFound",(await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.GetAsync(id))).Code);
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.PublishAsync(id));
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.DeleteDraftAsync(id));
    }
    [Fact]public async Task PublishRequiresFieldsAndPublishedCannotBeDeletedOrReopened()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.Recruitments.SaveDraftAsync(null,t.Input() with{Positions=[]});
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.PublishAsync(id));
        await t.Recruitments.SaveDraftAsync(id,t.Input());await t.Recruitments.PublishAsync(id);
        Assert.True((await t.Recruitments.GetAsync(id)).IsAvailable);
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.DeleteDraftAsync(id));
        await t.Recruitments.CloseAsync(id);
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.PublishAsync(id));
        Assert.False((await t.Recruitments.GetAsync(id)).IsAvailable);
    }
    [Fact]public async Task DateAndFeeValidationDoesNotCorruptDraft()
    {
        var t=new Scenario(db);await t.InitializeAsync();
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.SaveDraftAsync(null,t.Input() with{FeeKind=FeeKind.Paid,FeeAmount=-1}));
        await Assert.ThrowsAsync<DomainException>(()=>t.Recruitments.SaveDraftAsync(null,t.Input() with{StartsOn=new(2026,11,5),EndsOn=new(2026,11,1)}));
    }
}
