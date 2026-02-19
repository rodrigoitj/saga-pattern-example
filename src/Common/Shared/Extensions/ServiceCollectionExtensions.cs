using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var rabbitMqSection = configuration.GetSection("RabbitMQ");
        var rabbitMqHost = rabbitMqSection["Host"] ?? "localhost";
        var rabbitMqVirtualHost = rabbitMqSection["VirtualHost"] ?? "/";
        var rabbitMqUsername = rabbitMqSection["Username"] ?? "guest";
        var rabbitMqPassword = rabbitMqSection["Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
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
                }
            );
        });

        return services;
    }
}
