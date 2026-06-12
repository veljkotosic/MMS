using System.ComponentModel;

namespace MMS.Core.Filters.Halftone;

public sealed class HalftoneFilterOptions : IFilterOptions
{
    [Category("Halftone")]
    [DisplayName("Cell Size")]
    public int CellSize { get; set; } = 8;

    public string? Validate() 
    {
        return CellSize < 1 ? "Invalid cell size." : null;
    }
}
