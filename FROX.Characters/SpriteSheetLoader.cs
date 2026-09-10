using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FROX.Characters;

public sealed class SpriteSheetLoader
{
    public static ImageSource LoadSheet(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Character asset not found: {filePath}");
        }

        var source = new BitmapImage();
        source.BeginInit();
        source.CacheOption = BitmapCacheOption.OnLoad;
        source.UriSource = new Uri(filePath, UriKind.Absolute);
        source.EndInit();
        source.Freeze();
        return source;
    }

    public static CroppedBitmap? GetFrame(ImageSource source, int frameIndex, int frameWidth, int frameHeight)
    {
        if (source is not BitmapSource bitmapSource)
        {
            return null;
        }

        var totalFrames = bitmapSource.PixelWidth / frameWidth;
        var x = (frameIndex % totalFrames) * frameWidth;
        var y = 0;
        var rect = new Int32Rect(x, y, frameWidth, frameHeight);
        return new CroppedBitmap(bitmapSource, rect);
    }
}
