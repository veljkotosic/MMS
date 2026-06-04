using MMS.Application.Abstract.Command;
using MMS.Application.Abstract.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace MMS.Infrastructure.Dispatcher;

public sealed class Dispatcher : IDispatcher
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public Dispatcher(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task ExecuteAsync(ICommand command, CancellationToken cancellationToken)
    {
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(command.GetType());
        
        using var scope = _serviceScopeFactory.CreateScope();
        
        await HandleAsync(handlerType, scope, command, cancellationToken);
    }

    public async Task<TCommandResult> ExecuteAsync<TCommandResult>(ICommand<TCommandResult> command, CancellationToken cancellationToken) where TCommandResult : ICommandResult
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TCommandResult));
        
        using var scope = _serviceScopeFactory.CreateScope();
        
        return await HandleWithResultAsync<TCommandResult>(handlerType, scope, command, cancellationToken);
    }
    
    private async Task HandleAsync(Type handlerType, IServiceScope serviceScope, object dispatchingObject, CancellationToken cancellationToken = default)
    {
        dynamic handler = ResolveHandler(handlerType, serviceScope, dispatchingObject);
        
        await handler.HandleAsync((dynamic)dispatchingObject, cancellationToken);
    }
    
    private async Task<TResult> HandleWithResultAsync<TResult>(Type handlerType, IServiceScope serviceScope, object dispatchingObject, CancellationToken cancellationToken = default)
    {
        dynamic handler = ResolveHandler(handlerType, serviceScope, dispatchingObject);
        
        return await handler.HandleAsync((dynamic)dispatchingObject, cancellationToken);
    }
    
    private dynamic ResolveHandler(Type handlerType, IServiceScope serviceScope, object dispatchingObject)
    {
        dynamic handler;

        try
        {
            handler = serviceScope.ServiceProvider.GetRequiredService(handlerType);
        }
        catch (InvalidOperationException serviceProviderException)
        {
            throw new DispatchException(dispatchingObject, serviceProviderException);
        }

        return handler;
    }
}