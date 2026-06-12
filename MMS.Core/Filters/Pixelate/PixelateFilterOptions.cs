using System.ComponentModel;

namespace MMS.Core.Filters.Pixelate;

public sealed class PixelateFilterOptions : IFilterOptions
{
    [Category("Pixelate")]
    [DisplayName("Block Size")]
    public int BlockSize { get; set; } = 10;

    public string? Validate() 
    {
        return BlockSize < 1 ? "Pixelate block size must be at least 1." : null;
    }
}
