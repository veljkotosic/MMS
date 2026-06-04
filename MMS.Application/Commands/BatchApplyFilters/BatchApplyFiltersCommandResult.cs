using System.Drawing;
using MMS.Application.Abstract.Command;


namespace MMS.Application.Commands.BatchApplyFilters;

public record BatchApplyFiltersCommandResult(Bitmap Bitmap) : ICommandResult;
