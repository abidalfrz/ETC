using CommunityToolkit.Mvvm.ComponentModel;
namespace Etc.Desktop.Shared;
public sealed partial class SkillChoice(int id,string name,bool selected=false):ObservableObject
{
    public int Id {get;}=id;
    public string Name {get;}=name;
    [ObservableProperty]private bool isSelected=selected;
}
