using System.Drawing;

namespace MMS.Core.Filters.Abstract;

public interface IImageFilter
{
    void Execute(Bitmap bitmap);
}