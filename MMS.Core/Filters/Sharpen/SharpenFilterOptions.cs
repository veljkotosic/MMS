using System.ComponentModel;

namespace MMS.Core.Filters.Sharpen;

public sealed class SharpenFilterOptions
{
    [Category("Sharpening")]
    [DisplayName("Strength")]
    public double Strength { get; set; } = 1.0;
}
