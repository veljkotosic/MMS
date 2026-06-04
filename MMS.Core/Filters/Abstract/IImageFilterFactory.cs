using MMS.Core.Filters.Enums;

namespace MMS.Core.Filters.Abstract;

public interface IImageFilterFactory
{
    IImageFilter Create(ImageFilterType type);
}