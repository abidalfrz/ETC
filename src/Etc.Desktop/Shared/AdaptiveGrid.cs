using Avalonia.Controls;
namespace Etc.Desktop.Shared;
public sealed class AdaptiveGrid:Grid
{
    private double breakpoint=1100;
    public double Breakpoint{get=>breakpoint;set{if(breakpoint!=value){breakpoint=value;ArrangeColumns();}}}
    public int WideColumns{get;set;}=2;
    public int NarrowColumns{get;set;}=1;
    public double SideWidth{get;set;}=384;
    public bool ReverseWhenNarrow{get;set;}
    public AdaptiveGrid(){ColumnSpacing=24;RowSpacing=24;SizeChanged+=(_,_)=>ArrangeColumns();Loaded+=(_,_)=>ArrangeColumns();}
    private void ArrangeColumns()
    {
        var wide=Bounds.Width>=Breakpoint;var count=wide?WideColumns:NarrowColumns;
        var definition=wide&&count==2&&SideWidth>0?$"*,{SideWidth}":string.Join(',',Enumerable.Repeat("*",count));
        if(ColumnDefinitions.ToString()!=definition)ColumnDefinitions=new ColumnDefinitions(definition);
        RowDefinitions=new RowDefinitions(string.Join(',',Enumerable.Repeat("Auto",Math.Max(1,(Children.Count+count-1)/count))));
        for(int i=0;i<Children.Count;i++){var index=!wide&&ReverseWhenNarrow?Children.Count-1-i:i;SetColumn(Children[i],index%count);SetRow(Children[i],index/count);}
    }
}
