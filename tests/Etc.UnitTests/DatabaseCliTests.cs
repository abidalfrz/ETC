using Etc.Desktop;

namespace Etc.UnitTests;

[CollectionDefinition("CLI", DisableParallelization = true)]
public sealed class CliCollection;

[Collection("CLI")]
public sealed class DatabaseCliTests
{
    [Theory]
    [InlineData(null, "belum diatur")]
    [InlineData("   ", "belum diatur")]
    [InlineData("PrivateMarker_736=value;Password=PrivateMarker_736", "format")]
    [InlineData("Database=ETC_Demo;User ID=sa;Password=PrivateMarker_736", "Server harus diisi")]
    [InlineData("Server=127.0.0.1;User ID=sa;Password=PrivateMarker_736", "Database harus diisi")]
    public void SeedReportsConfigurationProblemBeforeConnectingWithoutLeakingSecrets(string? connectionString, string expectedReason)
    {
        var previousConnection = Environment.GetEnvironmentVariable("ETC_CONNECTION_STRING");
        var previousError = Console.Error;
        var previousOutput = Console.Out;
        using var error = new StringWriter();
        using var output = new StringWriter();
        try
        {
            Environment.SetEnvironmentVariable("ETC_CONNECTION_STRING", connectionString);
            Console.SetError(error);
            Console.SetOut(output);

            var exitCode = Program.Main(["--seed-demo"]);

            Assert.Equal(2, exitCode);
            Assert.Contains(expectedReason, error.ToString(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PrivateMarker_736", error.ToString() + output);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ETC_CONNECTION_STRING", previousConnection);
            Console.SetError(previousError);
            Console.SetOut(previousOutput);
        }
    }
}
