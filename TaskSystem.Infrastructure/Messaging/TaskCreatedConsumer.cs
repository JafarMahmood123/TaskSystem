using MassTransit;
using TaskSystem.Domain.Events;

namespace TaskSystem.Infrastructure.Messaging;

public class TaskCreatedConsumer : IConsumer<TaskCreatedEvent>
{
    public async Task Consume(ConsumeContext<TaskCreatedEvent> context)
    {
        var message = context.Message;
        
        // Simulate "Work" (like sending an email)
        Console.WriteLine($"[RabbitMQ] Real-time Notification: Task '{message.Title}' was created at {message.CreatedAt}");
        
        await Task.CompletedTask;
    }
}