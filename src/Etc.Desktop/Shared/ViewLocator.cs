using Avalonia.Controls;
using Avalonia.Controls.Templates;
namespace Etc.Desktop.Shared;
public sealed class ViewLocator:IDataTemplate
{
    public bool Match(object? data)=>data is PageViewModel;
    public Control? Build(object? data)
    {
        if(data is null)return null;
        var name=data.GetType().FullName!.Replace("ViewModel","View",StringComparison.Ordinal);
        var type=typeof(ViewLocator).Assembly.GetType(name)??throw new InvalidOperationException("View belum tersedia: "+name);
        return (Control)Activator.CreateInstance(type)!;
    }
}
