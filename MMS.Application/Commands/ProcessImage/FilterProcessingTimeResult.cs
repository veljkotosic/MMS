namespace MMS.Application.Commands.ProcessImage;

public record FilterProcessingTimeResult(
    int FilterIndex,
    string FilterName,
    long ProcessingTimeMs);