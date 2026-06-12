using System.Drawing;
using System.Drawing.Imaging;
using MMS.Core.ImageResource;
using MMS.Core.Utility;

namespace MMS.Core.FileManager;

public class StandardFileManager :
    IFileManager<StandardImageResource>,
    IFileManager<IImageResource>
{
    public StandardImageResource LoadImage(string path)
    {
        ApplicationLimits.ValidateFileSize(path);
        var bitmap = new Bitmap(path);

        try
        {
            ApplicationLimits.ValidateProcessedImageSize(bitmap.Width, bitmap.Height);
            return new StandardImageResource(bitmap);
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    public void SaveImage(string path, IImageResource data) => SaveImage(path, (StandardImageResource)data);

    public void SaveImage(string path, StandardImageResource data)
    {
        var extension = Path.GetExtension(path).ToLower();
        
        var format = extension switch
        {
            ".jpg" or ".jpeg" => ImageFormat.Jpeg,
            ".png" => ImageFormat.Png,
            ".bmp" => ImageFormat.Bmp,
            ".gif" => ImageFormat.Gif,
            _ => throw new NotSupportedException($"Unsupported file format: {extension}")
        };

        data.GetBitmap().Save(path, format);
    }

    IImageResource IFileManager<IImageResource>.LoadImage(string path) => LoadImage(path);
}
