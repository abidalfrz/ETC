using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class ExternalLinkTests
{
    [Theory][InlineData("file:///tmp/a")][InlineData("javascript:alert(1)")][InlineData("data:text/plain,hello")]
    public void NonHttpSchemesRejected(string raw)=>Assert.Throws<DomainException>(()=>ExternalLinkService.Validate(raw));
    [Fact]public async Task ValidUrlPreservedAndLaunchFailureIsSafe()
    {
        var uri=ExternalLinkService.Validate("https://example.test/a?q=1");Assert.Equal("example.test",uri?.Host);
        Assert.Null(ExternalLinkService.Validate(null));
        var svc=new ExternalLinkService(_=>Task.FromResult(false));
        Assert.Equal("Unavailable",(await Assert.ThrowsAsync<DomainException>(()=>svc.OpenAsync(uri!))).Code);
    }
}
