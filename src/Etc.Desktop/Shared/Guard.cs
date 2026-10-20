namespace Etc.Desktop.Shared;
public static class Guard
{
    public static string Text(string? value,string label,int maximum,bool required=true)
    {
        var text=value?.Trim()??"";
        if((required && text.Length==0)||text.Length>maximum)
            throw new DomainException("Validation",$"{label} {(text.Length==0?"wajib diisi":$"maksimal {maximum} karakter")}.");
        return text;
    }
    public static Uri? Url(string? value)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        if(value.Length>2048||!Uri.TryCreate(value.Trim(),UriKind.Absolute,out var uri)||
            (uri.Scheme!=Uri.UriSchemeHttp&&uri.Scheme!=Uri.UriSchemeHttps)||string.IsNullOrEmpty(uri.Host)||uri.UserInfo.Length>0)
            throw new DomainException("Validation","Tautan harus berupa URL HTTP atau HTTPS yang valid.");
        return uri;
    }
}
