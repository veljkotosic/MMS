using MMS.Application.Abstract.Command;
using MMS.Core.Filters;

namespace MMS.Application.Commands.ProcessImage;

public record ProcessImageCommand(
    int Width,
    int Height,
    byte[] ImageData,
    IReadOnlyList<IImageFilter> Filters) : ICommand<ProcessImageCommandResult>;
