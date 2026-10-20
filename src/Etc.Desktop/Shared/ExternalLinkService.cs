namespace Etc.Desktop.Shared;
public sealed class ExternalLinkService(Func<Uri,Task<bool>> launcher)
{
    public static Uri? Validate(string? raw)=>Guard.Url(raw);
    public async Task OpenAsync(Uri uri,CancellationToken ct=default)
    {
        var valid=Validate(uri.OriginalString)??throw new DomainException("Validation","Tautan belum tersedia.");ct.ThrowIfCancellationRequested();
        try{if(!await launcher(valid))throw new DomainException("Unavailable","Browser tidak dapat membuka tautan. Coba lagi.");}
        catch(Exception e)when(e is not(DomainException or OperationCanceledException)){throw new DomainException("Unavailable","Browser tidak dapat membuka tautan. Coba lagi.");}
    }
}
