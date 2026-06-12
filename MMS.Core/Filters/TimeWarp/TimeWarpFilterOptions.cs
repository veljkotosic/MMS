using System.ComponentModel;

namespace MMS.Core.Filters.TimeWarp;

public sealed class TimeWarpFilterOptions : IFilterOptions
{
    [Category("TimeWarp")]
    [DisplayName("Strength")]
    public double Strength { get; set; } = 1.0;

    [Category("TimeWarp")]
    [DisplayName("Radius")]
    public double Radius { get; set; } = 0.5;

    [Category("TimeWarp")]
    [DisplayName("U")]
    public double U { get; set; } = 0.5;

    [Category("TimeWarp")]
    [DisplayName("V")]
    public double V { get; set; } = 0.5;

    public string? Validate()
    {
        if (!double.IsFinite(Strength))
        {
            return "Invalid strength.";
        }

        return IsNormalized(U) && IsNormalized(V) && IsNormalized(Radius) ? null : "Invalid parameters.";
    }

    private static bool IsNormalized(double value)
    {
        return double.IsFinite(value) && value is >= 0 and <= 1;
    }
}
