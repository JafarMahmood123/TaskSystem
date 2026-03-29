using MassTransit;
using TaskSystem.Application.Abstractions;

namespace TaskSystem.Infrastructure.Messaging;

public class MessageBus : IMessageBus
{
    private readonly IPublishEndpoint _publishEndpoint;

    public byte[]? Body { get; set; }

    public MessageBus(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class
    {
        await _publishEndpoint.Publish(message, ct);
    }
}