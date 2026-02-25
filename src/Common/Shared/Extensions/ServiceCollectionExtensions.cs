using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Messaging.Configuration;
using Shared.Infrastructure.Messaging.Inbox;
using Shared.Infrastructure.Messaging.Outbox;

namespace Shared.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the outbox publisher, outbox processor background service, and inbox consume filter.
    /// The TDbContext must implement <see cref="IOutboxInboxDbContext"/>.
    /// </summary>
    public static IServiceCollection AddOutboxInbox<TDbContext>(this IServiceCollection services)
        where TDbContext : DbContext, IOutboxInboxDbContext
    {
        services.AddScoped<IOutboxInboxDbContext>(sp => sp.GetRequiredService<TDbContext>());
        services.AddScoped<IOutboxPublisher, OutboxPublisher>();
        services.AddScoped(typeof(InboxConsumeFilter<>));
        services.AddHostedService<OutboxProcessor>();
        return services;
    }

    public static IServiceCollection AddRabbitMqMessaging(
        this IServiceCollection services,
        IConfiguration configuration,
        string? endpointNamePrefix = null,
        Action<IBusRegistrationConfigurator>? configure = null,
        bool useInbox = false
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

                    if (useInbox)
                    {
                        cfg.UseConsumeFilter(typeof(InboxConsumeFilter<>), context);
                    }

                    cfg.ConfigureEndpoints(context);
                }
            );
        });

        return services;
    }
}
