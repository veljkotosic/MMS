namespace MMS.Infrastructure.Dispatcher;

public sealed class DispatchException : Exception
{
    public DispatchException(object dispatchingObject, Exception innerException)
        : base($"Error dispatching '{dispatchingObject.GetType().Name}' because of: {innerException.Message}", innerException)
    { }
}