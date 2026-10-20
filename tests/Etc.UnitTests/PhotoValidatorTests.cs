using Etc.Desktop.Features.Profiles;
using Etc.Desktop.Shared;
namespace Etc.UnitTests;
public sealed class PhotoValidatorTests
{
    [Fact]public void ValidPngDecodesAndExcessiveDimensionsAreRejected()
    {
        static byte[] Png(int width){using var bitmap=new SkiaSharp.SKBitmap(width,1);using var image=SkiaSharp.SKImage.FromBitmap(bitmap);using var data=image.Encode(SkiaSharp.SKEncodedImageFormat.Png,100);return data.ToArray();}
        PhotoValidator.Validate(Png(32));Assert.Throws<DomainException>(()=>PhotoValidator.Validate(Png(4097)));
    }
    [Fact]public void OversizedPhotoRejected()=>Assert.Throws<DomainException>(()=>PhotoValidator.Validate(new byte[2097153]));
    [Fact]public void CorruptPhotoRejected()=>Assert.Throws<DomainException>(()=>PhotoValidator.Validate([1,2,3,4]));
}
