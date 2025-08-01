using Microsoft.Extensions.DependencyInjection;
using DevOpsMcp.Application.Behaviors;

namespace DevOpsMcp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
            
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(RetryBehavior<,>));
        });

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        services.AddMemoryCache();
        
        return services;
    }
}