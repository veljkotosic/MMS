using System.ComponentModel;

namespace MMS.Core.Filters.EdgeDetection;

public sealed class EdgeDetectFilterOptions
{
    [Category("Detection")]
    [DisplayName("Direction")]
    public EdgeDetectDirection Direction { get; set; } = EdgeDetectDirection.Horizontal;
}
