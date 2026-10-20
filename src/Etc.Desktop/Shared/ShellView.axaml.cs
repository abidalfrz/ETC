using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System.ComponentModel;
namespace Etc.Desktop.Shared;
public sealed partial class ShellView:UserControl
{
    private ShellViewModel? model;
    public ShellView()
    {
        InitializeComponent();SizeChanged+=(_,_)=>{if(DataContext is ShellViewModel vm)vm.IsCompact=Bounds.Width<1100;};
        DataContextChanged+=(_,_)=>{if(model is not null)model.PropertyChanged-=Changed;model=DataContext as ShellViewModel;if(model is not null)model.PropertyChanged+=Changed;};
        AddHandler(KeyDownEvent,HandleMenuKeys,RoutingStrategies.Tunnel);
    }
    private Button[] MenuButtons()=>NavigationPanel.GetVisualDescendants().OfType<Button>().Where(b=>b.IsEffectivelyVisible&&b.IsEffectivelyEnabled).OrderBy(b=>b.CommandParameter is null?1:0).ToArray();
    private void Changed(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(ShellViewModel.IsMenuOpen)||model is not{IsCompact:true} current)return;
        Dispatcher.UIThread.Post(()=>{if(current.IsMenuOpen){MenuButtons().FirstOrDefault()?.Focus(NavigationMethod.Tab);MotionSettings.Fade(NavigationPanel,140);}else MenuToggle.Focus(NavigationMethod.Tab);},DispatcherPriority.Loaded);
    }
    private void HandleMenuKeys(object? sender,KeyEventArgs e)
    {
        if(model is not{IsCompact:true,IsMenuOpen:true} current)return;
        if(e.Key==Key.Escape){current.IsMenuOpen=false;e.Handled=true;}
        else if(e.Key==Key.Tab)
        {
            var buttons=MenuButtons();if(buttons.Length==0)return;var focused=TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();var index=Array.FindIndex(buttons,b=>ReferenceEquals(b,focused));
            buttons[(index+(e.KeyModifiers.HasFlag(KeyModifiers.Shift)?-1:1)+buttons.Length)%buttons.Length].Focus(NavigationMethod.Tab);e.Handled=true;
        }
    }
}
