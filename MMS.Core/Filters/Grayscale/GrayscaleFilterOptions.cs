using System.ComponentModel;

namespace MMS.Core.Filters.Grayscale;

public class GrayscaleFilterOptions : IFilterOptions
{
    [Category("Grayscale")]
    [DisplayName("Red Multiplier")]
    public int RMul { get; set; } = 306;

    [Category("Grayscale")]
    [DisplayName("Green Multiplier")]
    public int GMul { get; set; } = 601;

    [Category("Grayscale")]
    [DisplayName("Blue Multiplier")]
    public int BMul { get; set; } = 117;

    [Browsable(false)]
    public int Shift { get; set; } = 10;

    public string? Validate()
    {
        if (RMul < 0 || GMul < 0 || BMul < 0)
        {
            return "Invalid multipliers.";
        }

        return (long)RMul + GMul + BMul is < 1 or > 1024 ? "Invalid multiplier sum." : null;
    }
}
