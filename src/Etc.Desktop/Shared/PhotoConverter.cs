using System.Globalization;
using System.Xml.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
namespace Etc.Desktop.Shared;
public sealed class PhotoConverter:IValueConverter
{
    public object? Convert(object? value,Type targetType,object? parameter,CultureInfo culture)
    {
        if(value is not byte[] bytes)return null;
        try{using var stream=new MemoryStream(bytes);return new Bitmap(stream);}catch(ArgumentException){return null;}catch(InvalidOperationException){return null;}
    }
    public object ConvertBack(object? value,Type t,object? p,CultureInfo c)=>throw new NotSupportedException();
}
public sealed class IconConverter:IValueConverter
{
    private readonly Dictionary<string,Geometry> icons=new();
    public object? Convert(object? value,Type type,object? parameter,CultureInfo culture)
    {
        if(value is not string key)return null;if(icons.TryGetValue(key,out var cached))return cached;
        using var stream=AssetLoader.Open(new Uri($"avares://Etc.Desktop/Assets/Icons/{key}.svg"));var doc=XDocument.Load(stream);
        var paths=doc.Descendants().Where(x=>x.Name.LocalName=="path").Select(x=>x.Attribute("d")?.Value);
        var geometry=Geometry.Parse(string.Join(" ",paths));icons[key]=geometry;return geometry;
    }
    public object ConvertBack(object? v,Type t,object? p,CultureInfo c)=>throw new NotSupportedException();
}
