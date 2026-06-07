using Google.Protobuf;
using Grpc.Core;
using MMS.Application.Abstract.Dispatcher;
using MMS.Application.Commands.ProcessImage;
using MMS.Contracts;
using MMS.Core.Filters;
using MMS.Core.Filters.Grayscale;

namespace MMS.GrpcService.Services;

public sealed class ImageProcessorService(
    IDispatcher dispatcher,
    ILogger<ImageProcessorService> logger) : ImageProcessor.ImageProcessorBase
{
    private const int ChunkSize = 64 * 1024;

    public override async Task ProcessImage(
        IAsyncStreamReader<ProcessImageRequest> requestStream,
        IServerStreamWriter<ProcessImageResponse> responseStream,
        ServerCallContext context)
    {
        logger.LogInformation("Started image processing request {RequestId}.", context.GetHttpContext().TraceIdentifier);

        try
        {
            var command = await ParseCommandAsync(requestStream, context.CancellationToken);
            logger.LogInformation(
                "Parsed image processing request {RequestId}: {Width}x{Height}, {FilterCount} filter/s",
                context.GetHttpContext().TraceIdentifier,
                command.Width,
                command.Height,
                command.Filters.Count);

            var result = await dispatcher.ExecuteAsync(command, context.CancellationToken);

            await WriteResultAsync(responseStream, result, context.CancellationToken);

            logger.LogInformation(
                "Completed image processing request {RequestId}: {FilterCount} filters in {ProcessingTimeMs} ms.",
                context.GetHttpContext().TraceIdentifier,
                result.FilterTimes.Count,
                result.TotalProcessingTimeMs);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Image processing request {RequestId} failed.",
                context.GetHttpContext().TraceIdentifier);
            throw;
        }
    }

    private static async Task<ProcessImageCommand> ParseCommandAsync(
        IAsyncStreamReader<ProcessImageRequest> requestStream,
        CancellationToken cancellationToken)
    {
        ImageHeader? header = null;
        using var imageStream = new MemoryStream();

        await foreach (var request in requestStream.ReadAllAsync(cancellationToken))
        {
            switch (request.PayloadCase)
            {
                case ProcessImageRequest.PayloadOneofCase.Header when header == null:
                    header = request.Header;
                    break;
                case ProcessImageRequest.PayloadOneofCase.Header:
                    throw new ArgumentException("Duplicate header.");
                case ProcessImageRequest.PayloadOneofCase.Chunk:
                    await imageStream.WriteAsync(request.Chunk.Data.Memory, cancellationToken);
                    break;
                default:
                    throw new ArgumentException("Request payload is missing.");
            }
        }

        if (header == null)
        {
            throw new ArgumentException("Header is missing.");
        }

        return new ProcessImageCommand(
            header.Width,
            header.Height,
            imageStream.ToArray(),
            header.Filters.Select(ParseFilter).ToList());
    }

    private static IImageFilter ParseFilter(ImageFilter filter)
    {
        return filter.FilterCase switch
        {
            ImageFilter.FilterOneofCase.Grayscale =>
                new Core.Filters.Grayscale.GrayscaleFilter(new GrayscaleFilterOptions
                {
                    RMul = filter.Grayscale.RMul,
                    GMul = filter.Grayscale.GMul,
                    BMul = filter.Grayscale.BMul
                }),
            _ => throw new ArgumentException("Filter type is missing or unsupported.")
        };
    }

    private static async Task WriteResultAsync(
        IServerStreamWriter<ProcessImageResponse> responseStream,
        ProcessImageCommandResult result,
        CancellationToken cancellationToken)
    {
        var responseResult = new ProcessImageResult
        {
            TotalProcessingTimeMs = result.TotalProcessingTimeMs
        };

        responseResult.FilterTimes.Add(result.FilterTimes.Select(item => new FilterProcessingTime
        {
            FilterIndex = item.FilterIndex,
            FilterName = item.FilterName,
            ProcessingTimeMs = item.ProcessingTimeMs
        }));

        await responseStream.WriteAsync(
            new ProcessImageResponse { Result = responseResult },
            cancellationToken);

        for (var offset = 0; offset < result.ImageData.Length; offset += ChunkSize)
        {
            var length = Math.Min(ChunkSize, result.ImageData.Length - offset);
            await responseStream.WriteAsync(
                new ProcessImageResponse
                {
                    Chunk = new ImageChunk
                    {
                        Data = ByteString.CopyFrom(result.ImageData, offset, length)
                    }
                },
                cancellationToken);
        }
    }
}
