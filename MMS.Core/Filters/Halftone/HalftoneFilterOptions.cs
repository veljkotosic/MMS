using System.ComponentModel;

namespace MMS.Core.Filters.Halftone;

public sealed class HalftoneFilterOptions
{
    [Category("Halftone")]
    [DisplayName("Cell Size")]
    public int CellSize { get; set; } = 8;
}
