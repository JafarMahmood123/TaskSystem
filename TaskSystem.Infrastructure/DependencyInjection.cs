using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskSystem.Application.Abstractions;
using TaskSystem.Infrastructure.Caching;
using TaskSystem.Infrastructure.Messaging;
using TaskSystem.Infrastructure.Persistence;

namespace TaskSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. Setup PostgreSQL
        var connectionString = configuration.GetConnectionString("Postgres");
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        // 2. Register Repository
        services.AddScoped<ITaskRepository, TaskRepository>();

        // 3. Setup Redis
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // 4. Register Cache Service
        services.AddSingleton<ICacheService, RedisCacheService>();

        // 5. Setup RabbitMQ with MassTransit
        services.AddMassTransit(x =>
        {
            // Register the Consumer
            x.AddConsumer<TaskCreatedConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMQ"), h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddScoped<IMessageBus, MessageBus>();

        services.AddHostedService<OutboxProcessor>();

        return services;
    }
}