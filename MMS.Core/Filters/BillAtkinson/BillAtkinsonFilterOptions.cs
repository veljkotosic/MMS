using System.ComponentModel;

namespace MMS.Core.Filters.BillAtkinson;

public sealed class BillAtkinsonFilterOptions
{
    [Category("Dithering")]
    [DisplayName("Threshold")]
    public int Threshold
    {
        get;
        set => field = Math.Clamp(value, 0, 255);
    } = 128;
}
