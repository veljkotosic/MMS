using System.Drawing;

namespace MMS.Core.ImageResource;

public sealed class StandardImageResource
    : IImageResource
{
    private Bitmap _bitmap;

    public StandardImageResource(Bitmap bitmap)
    {
        this._bitmap = bitmap;
    }
    
    public Bitmap GetBitmap() => _bitmap;
    
    public void SetBitmap(Bitmap bitmap)
    {
        _bitmap = bitmap;
    }
}