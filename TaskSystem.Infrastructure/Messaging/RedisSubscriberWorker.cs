using Microsoft.Extensions.Hosting;
using TaskSystem.Application;
using TaskSystem.Application.Abstractions;

namespace TaskSystem.Infrastructure.Messaging;

public class RedisSubscriberWorker : BackgroundService
{
    private readonly ICacheService _cache;

    public RedisSubscriberWorker(ICacheService cache)
    {
        _cache = cache;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("--> Redis Subscriber Worker Listening...");

        await _cache.SubscribeAsync("task_updates", (message) => {
            // In a real app, this might refresh a local memory cache 
            // or push a message to a mobile app via Firebase.
            Console.WriteLine($"[Redis Pub/Sub Received] {message}");
        });

        // Keep alive
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(5000, stoppingToken);
        }
    }
}