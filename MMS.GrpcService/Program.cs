using MMS.GrpcService.Services;
using MMS.Infrastructure.DependencyInjection;
using MMS.Core.Utility;

namespace MMS.GrpcService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddMms();
        
        builder.Services.AddGrpc(options =>
        {
            options.MaxReceiveMessageSize = checked((int)ApplicationLimits.MaxDecodedImageBytes);
            options.MaxSendMessageSize = checked((int)ApplicationLimits.MaxDecodedImageBytes);
        });

        var app = builder.Build();

        app.MapGrpcService<ImageProcessorService>();
        app.MapGet("/",
            () =>
                "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        app.Run();
    }
}
