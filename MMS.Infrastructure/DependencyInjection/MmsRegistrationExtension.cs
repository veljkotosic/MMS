using Microsoft.Extensions.DependencyInjection;
using MMS.Application.Abstract.Dispatcher;
using MMS.Core.Filters;
using MMS.Core.Filters.Abstract;

namespace MMS.Infrastructure.DependencyInjection;

public static class MmsRegistrationExtension
{
    extension(IServiceCollection services)
    {
        public void AddMms()
        {
            services.AddCommandHandlersFromAssembly(typeof(IDispatcher).Assembly);
            
            services.AddDispatcher();

            services.AddSingleton<IImageFilterFactory, ImageFilterFactory>();
        }
    }
}