#if IOS
using nfloat = System.Runtime.InteropServices.NFloat;
#endif
namespace RecipeApp.Services;

/// <summary>Small JPEG copies keep the feed bounded and strip location/EXIF metadata.</summary>
public static class FeedPhoto
{
    public static async Task<string> PrepareAsync(Stream source)
    {
        using var buffer = new MemoryStream();
        var bytes = new byte[81920]; int read;
        while ((read = await source.ReadAsync(bytes)) > 0)
        {
            if (buffer.Length + read > 15 * 1024 * 1024) throw new InvalidOperationException("Choose a photo smaller than 15 MB.");
            await buffer.WriteAsync(bytes.AsMemory(0, read));
        }
        return await Task.Run(() => Compress(buffer.ToArray()));
    }
    private static string Compress(byte[] bytes)
    {
#if ANDROID
        using var dimensions = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
        Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, dimensions);
        if (dimensions.OutWidth <= 0 || dimensions.OutHeight <= 0 || dimensions.OutWidth > 30000 || dimensions.OutHeight > 30000)
            throw new InvalidOperationException("This image could not be read.");
        int sample = 1;
        while (dimensions.OutWidth / sample > 1024 || dimensions.OutHeight / sample > 1024) sample *= 2;
        using var options = new Android.Graphics.BitmapFactory.Options { InSampleSize = sample };
        using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length, options)
            ?? throw new InvalidOperationException("This image could not be read.");
        for (int quality = 80; quality >= 20; quality -= 15)
        {
            using var output = new MemoryStream();
            bitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, quality, output);
            if (output.Length <= 150000) return Convert.ToBase64String(output.ToArray());
        }
        throw new InvalidOperationException("Choose a simpler or smaller photo.");
#elif IOS
        using var data = Foundation.NSData.FromArray(bytes);
        using var image = UIKit.UIImage.LoadFromData(data) ?? throw new InvalidOperationException("This image could not be read.");
        var width = image.Size.Width * image.CurrentScale;
        var height = image.Size.Height * image.CurrentScale;
        if (width <= 0 || height <= 0 || width > 30000 || height > 30000) throw new InvalidOperationException("This image could not be read.");
        var ratio = Math.Min(1d, 1024d / Math.Max((double)width, (double)height));
        UIKit.UIGraphics.BeginImageContextWithOptions(new CoreGraphics.CGSize((double)width * ratio, (double)height * ratio), true, 1);
        try {
            image.Draw(new CoreGraphics.CGRect(0, 0, (double)width * ratio, (double)height * ratio));
            using var resized = UIKit.UIGraphics.GetImageFromCurrentImageContext();
            for (var quality = .8; quality >= .2; quality -= .15) {
                using var jpeg = resized.AsJPEG((nfloat)quality);
                if (jpeg != null && jpeg.Length <= 150000) return Convert.ToBase64String(jpeg.ToArray());
            }
        } finally { UIKit.UIGraphics.EndImageContext(); }
        throw new InvalidOperationException("Choose a simpler or smaller photo.");
#else
        throw new PlatformNotSupportedException();
#endif
    }
}
