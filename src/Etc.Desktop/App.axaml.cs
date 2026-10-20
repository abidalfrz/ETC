using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
namespace Etc.Desktop;
public sealed partial class App:Application
{
    public override void Initialize(){AvaloniaXamlLoader.Load(this);Resources["Etc.Motion.Feedback"]=Shared.MotionSettings.Duration(100);Resources["Etc.Motion.Panel"]=Shared.MotionSettings.Duration(140);Resources["Etc.Motion.Dialog"]=Shared.MotionSettings.Duration(120);Resources["Etc.Motion.Reduced"]=TimeSpan.Zero;}
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window=new MainWindow();var root=new CompositionRoot(window);window.DataContext=root.Shell;desktop.MainWindow=window;root.Start();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
