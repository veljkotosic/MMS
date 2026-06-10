using MMS.Application.Abstract.Command;

namespace MMS.Application.Commands.ProcessImage;

public record ProcessImageCommandResult(
    byte[] ImageData,
    long TotalProcessingTimeMs,
    IReadOnlyList<FilterProcessingTimeResult> FilterTimes) 
    : ICommandResult;
