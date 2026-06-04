using System.Drawing;

namespace MMS.Core.ImageResource;

public interface IImageResource
{
    Bitmap GetBitmap();
    void SetBitmap(Bitmap bitmap);
}