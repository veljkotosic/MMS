using System.Drawing;

namespace MMS.Core.Filters;

public interface IImageFilter
{
    void Execute(Bitmap bitmap);
}