using Microsoft.AspNetCore.Mvc;
using TaskSystem.Application.Abstractions;
using TaskSystem.Domain.Entities;
using TaskSystem.Domain.Events;

namespace TaskSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _repository;
    private readonly ICacheService _cache;
    private readonly IMessageBus _messageBus;
    private const string TasksCacheKey = "all_tasks_key";

    public TasksController(ITaskRepository repository, ICacheService cache, IMessageBus messageBus)
    {
        _repository = repository;
        _cache = cache;
        _messageBus = messageBus;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // Try to get from Redis
        var cachedTasks = await _cache.GetAsync<IEnumerable<TaskItem>>(TasksCacheKey);
        if (cachedTasks != null) return Ok(cachedTasks);

        // If not in Redis, get from Postgres
        var tasks = await _repository.GetAllAsync();

        // Save to Redis for 1 minute
        await _cache.SetAsync(TasksCacheKey, tasks, TimeSpan.FromMinutes(1));

        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TaskItem task)
    {
        // Save to Postgres
        var createdTask = await _repository.CreateAsync(task);
    
        // Clear Redis Cache
        await _cache.RemoveAsync(TasksCacheKey);

        // Publish to RabbitMQ 
        await _messageBus.PublishAsync(new TaskCreatedEvent 
        { 
            Id = createdTask.Id, 
            Title = createdTask.Title, 
            CreatedAt = createdTask.CreatedAt 
        });

        return Ok(createdTask);
    }
}