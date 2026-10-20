using Etc.Desktop.Shared;
using SkiaSharp;
namespace Etc.Desktop.Features.Profiles;
public static class PhotoValidator
{
    public static void Validate(byte[] bytes)
    {
        if(bytes.Length>2097152)throw new DomainException("Validation","Foto maksimal 2 MiB.");
        using var data=SKData.CreateCopy(bytes);
        using var codec=SKCodec.Create(data);
        if(codec is null || codec.EncodedFormat is not(SKEncodedImageFormat.Png or SKEncodedImageFormat.Jpeg) || codec.Info.Width>4096 || codec.Info.Height>4096)
            throw new DomainException("Validation","Gunakan foto PNG/JPEG valid, maksimal 4096 × 4096.");
        using var bitmap=SKBitmap.Decode(bytes);
        if(bitmap is null)throw new DomainException("Validation","Foto tidak dapat dibaca.");
    }
}
