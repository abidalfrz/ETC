using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
namespace Etc.Desktop.Shared;
public static class MotionSettings
{
    // GNOME is read once at startup; unknown preferences keep motion disabled.
    private static readonly Lazy<bool> linuxEnabled=new(()=>ReadPreference(ReadGnomeSetting));
    public static bool Enabled=>OperatingSystem.IsWindows()
        ?SystemParametersInfo(0x1042,0,out var enabled,0)&&enabled
        :OperatingSystem.IsLinux()&&linuxEnabled.Value;
    public static bool ReadPreference(Func<string?> read)
    {
        try{return bool.TryParse(read()?.Trim(),out var enabled)&&enabled;}
        catch(Exception e)when(e is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException){return false;}
    }
    private static string? ReadGnomeSetting()
    {
        var start=new ProcessStartInfo("gsettings"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"get","org.gnome.desktop.interface","enable-animations"})start.ArgumentList.Add(arg);
        using var process=Process.Start(start);
        if(process is null)return null;
        if(!process.WaitForExit(500)){process.Kill(entireProcessTree:true);return null;}
        return process.ExitCode==0?process.StandardOutput.ReadToEnd():null;
    }
    public static TimeSpan Duration(int milliseconds)=>Enabled?TimeSpan.FromMilliseconds(milliseconds):TimeSpan.Zero;
    public static void Fade(Control control,int milliseconds)
    {
        if(!Enabled)return;
        var animation=new Animation{Duration=Duration(milliseconds),Easing=new LinearEasing(),Children={new KeyFrame{Cue=new Cue(0),Setters={new Setter(Avalonia.Visual.OpacityProperty,0d)}},new KeyFrame{Cue=new Cue(1),Setters={new Setter(Avalonia.Visual.OpacityProperty,1d)}}}};
        _=animation.RunAsync(control);
    }
    [DllImport("user32.dll",EntryPoint="SystemParametersInfoW")]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool SystemParametersInfo(uint action,uint parameter,[MarshalAs(UnmanagedType.Bool)]out bool value,uint flags);
}
