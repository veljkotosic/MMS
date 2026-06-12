namespace MMS.Core.Utility;

public static class ApplicationLimits
{
    public const long MaxFileBytes = 25L * 1024 * 1024;
    public const long MaxDecodedImageBytes = MaxFileBytes;
    public const int BytesPerProcessedPixel = 4;

    public static long ValidateProcessedImageSize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException("Invalid image dimensions.");
        }

        var size = checked((long)width * height * BytesPerProcessedPixel);

        if (size > MaxDecodedImageBytes)
        {
            throw new InvalidDataException("Decoded image exceeds the 25 MB limit.");
        }

        return size;
    }

    public static void ValidateFileSize(string path)
    {
        ValidateFileSize(new FileInfo(path).Length);
    }

    public static void ValidateFileSize(long length)
    {
        if (length > MaxFileBytes)
        {
            throw new InvalidDataException("File exceeds the 25 MB limit.");
        }
    }
}
