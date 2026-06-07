using System.ComponentModel;

namespace MMS.Core.Filters.Grayscale;

public class GrayscaleFilterOptions
{
    [Category("Weights")]
    [DisplayName("Red Multiplier")]
    public int RMul { get; set; } = 306;

    [Category("Weights")]
    [DisplayName("Green Multiplier")]
    public int GMul { get; set; } = 601;

    [Category("Weights")]
    [DisplayName("Blue Multiplier")]
    public int BMul { get; set; } = 117;

    [Browsable(false)]
    public int Shift { get; set; } = 10;
}
