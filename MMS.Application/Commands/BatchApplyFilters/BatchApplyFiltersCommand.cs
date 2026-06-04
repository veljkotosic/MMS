using System.Drawing;
using MMS.Application.Abstract.Command;

using MMS.Core.Filters.Enums;

namespace MMS.Application.Commands.BatchApplyFilters;

public record BatchApplyFiltersCommand(Bitmap Bitmap, List<ImageFilterType> Filters) : ICommand<BatchApplyFiltersCommandResult>;
