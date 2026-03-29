namespace TaskSystem.Domain.Events;

public record TaskCreatedEvent
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}