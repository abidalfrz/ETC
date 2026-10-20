using Etc.Desktop.Demo;
using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class DemoSeederTests
{
    [Theory][InlineData("master")][InlineData("ETC_Test")][InlineData("CustomerProduction")]
    public void SeedRejectsNonDemoDatabase(string name)=>Assert.Throws<DomainException>(()=>DemoSeeder.EnsureDemoTarget(name));
    [Fact]public void ExplicitDemoDatabaseAccepted()=>DemoSeeder.EnsureDemoTarget("ETC_Demo");
}
