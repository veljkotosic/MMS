using System.Drawing;
using MMS.Core.ImageResource;

namespace MMS.Core.FileManager;

public interface IFileManager<T>
    where T : IImageResource
{
    T LoadImage(string path);
    void SaveImage(string path, T data);
}