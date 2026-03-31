using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;
using StackExchange.Redis;
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

        // Inside AddInfrastructure method:
        var redisConnectionString = configuration.GetConnectionString("Redis") ?? "localhost:6379";
        var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);
        services.AddSingleton<IConnectionMultiplexer>(redisConnection);

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
        });

        services.AddHostedService<RedisSubscriberWorker>();

        services.AddHealthChecks()
    .AddNpgSql(
        connectionString: configuration.GetConnectionString("Postgres")!,
        name: "PostgreSQL",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgres" })
    .AddRedis(
        redisConnectionString: configuration.GetConnectionString("Redis")!,
        name: "Redis",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "cache", "redis" })
    .AddRabbitMQ(
        factory: sp =>
        {
            var factory = new ConnectionFactory()
            {
                Uri = new Uri($"amqp://guest:guest@{configuration.GetConnectionString("RabbitMQ")}/")
            };
            return factory.CreateConnectionAsync();
        },
        name: "RabbitMQ",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "broker", "rabbitmq" });

        return services;
    }
}