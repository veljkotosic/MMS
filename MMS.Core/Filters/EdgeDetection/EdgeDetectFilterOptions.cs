using System.ComponentModel;

namespace MMS.Core.Filters.EdgeDetection;

public sealed class EdgeDetectFilterOptions : IFilterOptions
{
    [Category("EdgeDetect")]
    [DisplayName("Direction")]
    public EdgeDetectDirection Direction { get; set; } = EdgeDetectDirection.Horizontal;

    public string? Validate()
    {
        return Enum.IsDefined(Direction) ? null : "Invalid direction.";
    }
}
