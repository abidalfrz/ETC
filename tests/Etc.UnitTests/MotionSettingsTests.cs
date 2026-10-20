using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class MotionSettingsTests
{
    [Theory]
    [InlineData("true",true)]
    [InlineData(" true\n",true)]
    [InlineData("false",false)]
    [InlineData(null,false)]
    [InlineData("",false)]
    [InlineData("unexpected output",false)]
    public void UnavailableOrDisabledPreferenceNeverEnablesMotion(string? value,bool expected)
        =>Assert.Equal(expected,MotionSettings.ReadPreference(()=>value));

    [Fact]public void InaccessiblePreferenceFallsBackToReducedMotion()
        =>Assert.False(MotionSettings.ReadPreference(()=>throw new System.ComponentModel.Win32Exception("settings unavailable")));
}
