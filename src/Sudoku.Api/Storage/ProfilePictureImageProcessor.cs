using SkiaSharp;

namespace Sudoku.Api.Storage;

public static class ProfilePictureImageProcessor
{
    public static byte[] NormalizeToPng(Stream input, int size)
    {
        using var source = SKBitmap.Decode(input);
        if (source is null || source.Width == 0 || source.Height == 0)
        {
            throw new ProfilePictureException("Upload a valid PNG, JPEG, or WebP image.");
        }

        var side = Math.Min(source.Width, source.Height);
        var sourceRect = SKRectI.Create(
            (source.Width - side) / 2,
            (source.Height - side) / 2,
            side,
            side);
        using var target = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawBitmap(
            source,
            sourceRect,
            new SKRect(0, 0, size, size));

        using var image = SKImage.FromBitmap(target);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, quality: 90);
        return encoded.ToArray();
    }
}
