using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using System.ComponentModel;
namespace Etc.Desktop.Features.Applications;
public sealed partial class ApplicantsView:UserControl
{
    private ApplicantsViewModel? model;
    public ApplicantsView()
    {
        InitializeComponent();DataContextChanged+=(_,_)=>Attach();Loaded+=(_,_)=>Attach();Unloaded+=(_,_)=>{if(model is not null)model.PropertyChanged-=Changed;};SizeChanged+=(_,_)=>Adapt();
        AddHandler(ListBox.SelectionChangedEvent,(_,e)=>{if(e.Source is ListBox{IsKeyboardFocusWithin:true})Shared.MotionSettings.Fade(ApplicantDetail,140);});
        KeyDown+=(_,e)=>{if(e.Key==Key.Escape&&model is{IsCompact:true,IsReviewingApplicant:true}){model.BackToListCommand.Execute(null);e.Handled=true;}};
    }
    private void Attach()
    {
        if(model is not null)model.PropertyChanged-=Changed;model=DataContext as ApplicantsViewModel;if(model is not null)model.PropertyChanged+=Changed;Adapt();
    }
    private void Adapt(){if(model is not null)model.IsCompact=TopLevel.GetTopLevel(this)?.Bounds.Width<1100;}
    private void Changed(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName!=nameof(ApplicantsViewModel.IsReviewingApplicant)||model is not{IsCompact:true} current)return;
        Dispatcher.UIThread.Post(()=>{if(!ReferenceEquals(model,current))return;if(current.IsReviewingApplicant){BackToApplicants.Focus();Shared.MotionSettings.Fade(ApplicantDetail,140);}else{current.ScrollOffset=current.ListScrollOffset;ApplicantList.Focus();}},DispatcherPriority.Loaded);
    }
}
