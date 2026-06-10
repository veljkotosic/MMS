using System.ComponentModel;

namespace MMS.Core.Filters.Gamma;

public sealed class GammaFilterOptions
{
    [Category("Correction")]
    [DisplayName("Gamma")]
    public double Gamma { get; set; } = 1.0;
}
