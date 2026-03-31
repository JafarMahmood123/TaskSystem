using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using TaskSystem.Infrastructure.Persistence;
using System.Text.Json;
using TaskSystem.Domain.Events;
using TaskSystem.Application.Abstractions;

namespace TaskSystem.Infrastructure.Messaging;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OutboxProcessor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

            // 1. Get unprocessed messages
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                try {
                    // 2. Publish to RabbitMQ
                    var eventData = JsonSerializer.Deserialize<TaskCreatedEvent>(message.Content);
                    if (eventData != null) {
                        await messageBus.PublishAsync(eventData, stoppingToken);
                    }

                    // 3. Mark as processed
                    message.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex) {
                    Console.WriteLine($"Error processing outbox message {message.Id}: {ex.Message}");
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);

            // Wait 5 seconds before checking again
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}