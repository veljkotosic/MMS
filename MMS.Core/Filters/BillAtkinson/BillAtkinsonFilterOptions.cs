using System.ComponentModel;

namespace MMS.Core.Filters.BillAtkinson;

public sealed class BillAtkinsonFilterOptions : IFilterOptions
{
    [Category("BillAtkinson")]
    [DisplayName("Threshold")]
    public int Threshold { get; set; } = 128;

    public string? Validate()
    {
        return Threshold is < 0 or > 255 ? "Invalid threshold." : null;
    }
}
