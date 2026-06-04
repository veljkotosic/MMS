using MMS.Core.Filters.Abstract;
using MMS.Core.Filters.Enums;
using MMS.Core.Filters.Grayscale;

namespace MMS.Core.Filters;

public class ImageFilterFactory : IImageFilterFactory
{
    public IImageFilter Create(ImageFilterType type)
    {
        return type switch
        {
            ImageFilterType.Grayscale => new GrayscaleFilter(new GrayscaleFilterOptions()),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}
