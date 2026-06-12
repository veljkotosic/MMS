using System.ComponentModel;

namespace MMS.Core.Filters.Gamma;

public sealed class GammaFilterOptions : IFilterOptions
{
    [Category("Gamma")]
    [DisplayName("Gamma")]
    public double Gamma { get; set; } = 1.0;

    public string? Validate() 
    {
        return !double.IsFinite(Gamma) || Gamma <= 0 ? "Invalid gamma." : null;
    }
}
