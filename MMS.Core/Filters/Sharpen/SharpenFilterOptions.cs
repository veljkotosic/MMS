using System.ComponentModel;

namespace MMS.Core.Filters.Sharpen;

public sealed class SharpenFilterOptions : IFilterOptions
{
    private const double MaxStrength = (int.MaxValue / 5.0) / 1024.0;

    [Category("Sharpen")]
    [DisplayName("Strength")]
    public double Strength { get; set; } = 1.0;

    public string? Validate() 
    {
        return !double.IsFinite(Strength) || Strength < 0 || Strength > MaxStrength ? "Invalid strength." : null;
    }
}
