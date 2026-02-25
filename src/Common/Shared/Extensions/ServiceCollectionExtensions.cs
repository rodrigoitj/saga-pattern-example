using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string? endpointNamePrefix = null,
        Action<IBusRegistrationConfigurator>? configure = null
    )
    {
        var rabbitMqSection = configuration.GetSection("RabbitMQ");
        var rabbitMqHost = rabbitMqSection["Host"] ?? "localhost";
        var rabbitMqVirtualHost = rabbitMqSection["VirtualHost"] ?? "/";
        var rabbitMqUsername = rabbitMqSection["Username"] ?? "guest";
        var rabbitMqPassword = rabbitMqSection["Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            if (!string.IsNullOrWhiteSpace(endpointNamePrefix))
            {
                x.SetEndpointNameFormatter(
                    new KebabCaseEndpointNameFormatter(endpointNamePrefix, false)
                );
            }

            configure?.Invoke(x);
            x.UsingRabbitMq(
                (context, cfg) =>
                {
                    cfg.Host(
                        rabbitMqHost,
                        rabbitMqVirtualHost,
                        h =>
                        {
                            h.Username(rabbitMqUsername);
                            h.Password(rabbitMqPassword);
                        }
                    );

                    cfg.UseMessageRetry(r =>
                        r.Incremental(
                                5,
                                TimeSpan.FromMilliseconds(200),
                                TimeSpan.FromMilliseconds(200)
                            )
                            .Handle<DbUpdateConcurrencyException>()
                    );

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        return services;
    }
}
