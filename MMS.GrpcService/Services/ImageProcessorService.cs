using Grpc.Core;
using MMS.Application.Abstract.Dispatcher;
using MMS.Application.Commands.BatchApplyFilters;
using MMS.Contracts;
using MMS.Core.Filters.Enums;
using System.Runtime.InteropServices;
using Google.Protobuf;
using System.Drawing;
using System.Drawing.Imaging;

namespace MMS.GrpcService.Services;

public class ImageProcessorService : ImageProcessor.ImageProcessorBase
{
    private readonly IDispatcher _dispatcher;

    public ImageProcessorService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public override async Task ProcessImage(
        IAsyncStreamReader<ProcessImageRequest> requestStream,
        IServerStreamWriter<ProcessImageResponse> responseStream,
        ServerCallContext context)
    {
        ImageHeader? header = null;
        using var ms = new MemoryStream();
        try
        {
            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                switch (request.PayloadCase)
                {
                    case ProcessImageRequest.PayloadOneofCase.Header: 
                        header = request.Header; 
                        break;
                    case ProcessImageRequest.PayloadOneofCase.Chunk:
                        await ms.WriteAsync(request.Chunk.Data.Memory, context.CancellationToken); 
                        break;
                }
            }

            if (header == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Header is missing"));
            }

            int width = header.Width;
            int height = header.Height;
            int bytesPerPixel = 4;
            long expectedSize = (long)width * height * bytesPerPixel;
            
            if (ms.Length != expectedSize)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid image data size. Expected {expectedSize}, got {ms.Length}"));
            }

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, width, height);
            var bmpData = bitmap.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            
            try
            {
                byte[] bytes = ms.ToArray();
                Marshal.Copy(bytes, 0, bmpData.Scan0, bytes.Length);
            }
            finally
            {
                bitmap.UnlockBits(bmpData);
            }

            var filters = header.Filters.Select(MapFilterType).ToList();
            
            var command = new BatchApplyFiltersCommand(bitmap, filters);
            var result = await _dispatcher.ExecuteAsync(command, context.CancellationToken);
            
            var resultBitmap = result.Bitmap;
            var resultBmpData = resultBitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            byte[] resultBytes = new byte[expectedSize];
            
            try
            {
                Marshal.Copy(resultBmpData.Scan0, resultBytes, 0, resultBytes.Length);
            }
            finally
            {
                resultBitmap.UnlockBits(resultBmpData);
            }

            int chunkSize = 64 * 1024;
            
            for (int i = 0; i < resultBytes.Length; i += chunkSize)
            {
                int length = Math.Min(chunkSize, resultBytes.Length - i);
                await responseStream.WriteAsync(new ProcessImageResponse
                {
                    Chunk = ByteString.CopyFrom(resultBytes, i, length)
                });
            }
        }
        catch (OperationCanceledException)
        {
            
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, ex.Message));
        }
    }

    private static ImageFilterType MapFilterType(FilterType type)
    {
        return type switch
        {
            FilterType.Grayscale => ImageFilterType.Grayscale,
            _ => ImageFilterType.Unknown
        };
    }
}