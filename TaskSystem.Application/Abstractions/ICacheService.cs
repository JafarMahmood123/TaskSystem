namespace TaskSystem.Application.Abstractions;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    
    // Add Pub/Sub methods
    Task PublishAsync(string channel, string message);
    Task SubscribeAsync(string channel, Action<string> handler);
}