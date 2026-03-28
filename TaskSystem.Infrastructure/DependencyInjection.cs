using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskSystem.Application.Abstractions;
using TaskSystem.Infrastructure.Caching;
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

        return services;
    }
}