using Avalonia.Controls;
using Avalonia.VisualTree;
using Etc.Desktop.Shared;
namespace Etc.Desktop.Features.Recruitments;
public sealed partial class RecruitmentFormView:UserControl
{
    public RecruitmentFormView(){InitializeComponent();Loaded+=(_,_)=>Adapt();SizeChanged+=(_,_)=>Adapt();}
    private void Adapt(){var compact=TopLevel.GetTopLevel(this)?.Bounds.Width<1100;foreach(var grid in this.GetVisualDescendants().OfType<AdaptiveGrid>())grid.Breakpoint=compact?double.MaxValue:700;}
}
