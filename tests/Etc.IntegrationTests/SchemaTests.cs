namespace Etc.IntegrationTests;
[Collection("SQL")]
public sealed class SchemaTests(DatabaseFixture db)
{
    [Fact]
    public async Task SchemaCreatesRequiredRelationalObjects()
    {
        var count = await db.ScalarAsync("SELECT COUNT(*) FROM sys.tables WHERE name IN ('Students','Recruitments','Positions','Applications','Bookmarks','Skills','StudentSkills','PositionSkills','Mentors','MentorSkills','SchemaVersion')");
        Assert.Equal(11, Convert.ToInt32(count));
    }
    [Theory]
    [InlineData("ETC_Demo")]
    [InlineData("master")]
    public void TestFixtureRefusesNonTestDatabase(string name) => Assert.Throws<InvalidOperationException>(() => DatabaseFixture.EnsureTestDatabase(name));
    [Fact]
    public async Task InitializerPreservesData()
    {
        var count = Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Skills"));
        await new Etc.Desktop.Infrastructure.DatabaseInitializer(db.Factory).InitializeAsync();
        Assert.Equal(count, Convert.ToInt32(await db.ScalarAsync("SELECT COUNT(*) FROM Skills")));
    }
    [Fact]public async Task ConstraintsRejectOrphanPositionInvalidCapacityAndDuplicateSkill()
    {
        var t=new Scenario(db);await t.InitializeAsync();var id=await t.OpenAsync();
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.ScalarAsync("INSERT Positions(RecruitmentId,Name,Capacity) VALUES(2147483647,N'Orphan',1)"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.ScalarAsync($"INSERT Positions(RecruitmentId,Name,Capacity) VALUES({id},N'Invalid',0)"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.ScalarAsync($"INSERT StudentSkills VALUES({t.A.Id},{t.SkillId})"));
    }
    [Fact]public async Task CrossRecruitmentPositionAndNegativeFeeAreRejectedBySql()
    {
        var t=new Scenario(db);await t.InitializeAsync();var first=await t.OpenAsync();var second=await t.OpenAsync();var pos=(await t.Recruitments.GetAsync(first)).Positions[0].Id;
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.ScalarAsync($"INSERT Applications(StudentId,RecruitmentId,PositionId,Message,SubmittedAtUtc) VALUES({t.B.Id},{second},{pos},N'Invalid',SYSUTCDATETIME())"));
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.ScalarAsync($"UPDATE Recruitments SET FeeAmount=-1 WHERE Id={first}"));
    }
}
