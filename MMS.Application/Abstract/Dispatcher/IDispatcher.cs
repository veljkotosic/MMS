using MMS.Application.Abstract.Command;

namespace MMS.Application.Abstract.Dispatcher;

public interface IDispatcher
{
    Task ExecuteAsync(ICommand command, CancellationToken cancellationToken);
    
    Task<TCommandResult> ExecuteAsync<TCommandResult>(ICommand<TCommandResult> command, CancellationToken cancellationToken)
        where TCommandResult : ICommandResult;
}