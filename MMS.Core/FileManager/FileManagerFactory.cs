using MMS.Core.ImageResource;

namespace MMS.Core.FileManager;

public static class FileManagerFactory
{
    public static IFileManager<IImageResource> GetFileManager(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();

        return extension switch
        {
            ".mms" => new MmsFileManager(),
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" => new StandardFileManager(),
            _ => throw new NotSupportedException($"Unsupported file format: {extension}")
        };
    }
}