using Microsoft.Extensions.DependencyInjection;
using MMS.Application.Abstract.Dispatcher;

namespace MMS.Infrastructure.DependencyInjection;

public static class DispatcherRegistrationExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDispatcher()
        {
            services.AddSingleton<IDispatcher, Dispatcher.Dispatcher>();
        
            return services;
        }
    }
}