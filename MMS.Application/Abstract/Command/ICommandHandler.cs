namespace MMS.Application.Abstract.Command;

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand, TCommandResult>
    where TCommand : ICommand<TCommandResult>
    where TCommandResult : ICommandResult
{
    Task<TCommandResult> HandleAsync(TCommand command, CancellationToken cancellationToken);
}