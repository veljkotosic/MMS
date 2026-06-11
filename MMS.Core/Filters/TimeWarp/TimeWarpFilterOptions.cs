using System.ComponentModel;

namespace MMS.Core.Filters.TimeWarp;

public sealed class TimeWarpFilterOptions
{
    [Category("Distortion")]
    [DisplayName("Strength")]
    public double Strength { get; set; } = 1.0;

    [Category("Distortion")]
    [DisplayName("Radius")]
    public double Radius
    {
        get;
        set => field = Math.Clamp(value, 0.0, 1.0);
    } = 0.5;

    [Category("Center")]
    [DisplayName("U")]
    public double U
    {
        get;
        set => field = Math.Clamp(value, 0.0, 1.0);
    } = 0.5;

    [Category("Center")]
    [DisplayName("V")]
    public double V
    {
        get;
        set => field = Math.Clamp(value, 0.0, 1.0);
    } = 0.5;
}
