using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using MMS.Application.Abstract.Command;

namespace MMS.Infrastructure.DependencyInjection;

public static class CommandHandlerRegistrationExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCommandHandlersFromAssembly(Assembly assembly)
        {
            var commandHandlerInterfaces = new[]
            {
                typeof(ICommandHandler<>),
                typeof(ICommandHandler<,>),
            };
            
            var types = assembly.GetTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false });
            
            foreach (var type in types)
            {
                var interfaces = type.GetInterfaces();

                foreach (var @interface in interfaces)
                {
                    if (!@interface.IsGenericType)
                        continue;

                    var genericDef = @interface.GetGenericTypeDefinition();

                    if (!commandHandlerInterfaces.Contains(genericDef))
                        continue;
                
                    services.AddScoped(@interface, type);
                }
            }
            
            return services;
        }
    }
}