using System.ComponentModel;

namespace MMS.Core.Filters.Pixelate;

public sealed class PixelateFilterOptions
{
    [Category("Pixelation")]
    [DisplayName("Block Size")]
    public int BlockSize { get; set; } = 10;
}
