using Etc.Desktop.Features.Recruitments;
namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class RecruitmentQueryTests(DatabaseFixture db)
{
    [Fact]public async Task SearchSummaryExposesSkillsAndLiveQuota()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(id)).Positions[0];
        var row=(await new RecruitmentQueries(db.Factory,t.Session,t.Clock).SearchAsync(new("",null,null,null,false,1))).Items.Single();
        Assert.Contains(pos.SkillText,row.PositionsText);Assert.Contains("0 dari 1 anggota diterima",row.PositionsText);Assert.Contains("1 tempat tersisa",row.PositionsText);
    }
    [Fact]public async Task RemovedLastResultReturnsToValidPage()
    {
        var t=new Scenario(db);await t.InitializeAsync();int last=0;for(int i=0;i<13;i++)last=await t.OpenAsync();
        var queries=new RecruitmentQueries(db.Factory,t.Session,t.Clock);var before=await queries.SearchAsync(new("",null,null,null,true,2));Assert.Equal(2,before.PageNumber);Assert.Single(before.Items);
        await t.Recruitments.CloseAsync(last);var after=await queries.SearchAsync(new("",null,null,null,true,2));Assert.Equal(1,after.PageNumber);Assert.Equal(12,after.Total);Assert.Equal(12,after.Items.Count);
    }
    [Fact]public async Task FiltersIntersectExcludeDraftsAndEscapeWildcards()
    {
        var t=new Scenario(db);await t.InitializeAsync();
        var id=await t.Recruitments.SaveDraftAsync(null,t.Input("Proyek 100% 'air' 日本語"));await t.Recruitments.PublishAsync(id);
        await t.Recruitments.SaveDraftAsync(null,t.Input("Draft 100%"));
        var service=new RecruitmentQueries(db.Factory,t.Session,t.Clock);
        var rows=await service.SearchAsync(new("%",RecruitmentCategory.Project,t.SkillId,"Backend",true,100));
        Assert.Single(rows.Items);Assert.Equal(id,rows.Items[0].Id);Assert.Equal(1,rows.PageNumber);
        Assert.Empty((await service.SearchAsync(new("",RecruitmentCategory.Competition,null,null,false,1))).Items);
        await t.Recruitments.CloseAsync(id);
        Assert.Empty((await service.SearchAsync(new("",null,null,null,true,1))).Items);
    }
    [Fact]public async Task EqualPublishTimesUseIdTieBreaker()
    {
        var t=new Scenario(db);await t.InitializeAsync();var a=await t.OpenAsync();var b=await t.OpenAsync();
        var rows=await new RecruitmentQueries(db.Factory,t.Session,t.Clock).SearchAsync(new("",null,null,null,false,1));
        Assert.Equal(new[]{b,a},rows.Items.Select(x=>x.Id));
    }
}
