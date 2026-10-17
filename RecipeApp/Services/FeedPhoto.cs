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
#else
        throw new PlatformNotSupportedException();
#endif
    }
}
