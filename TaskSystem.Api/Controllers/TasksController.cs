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
        // The repository handles saving the task AND the outbox message in one transaction
        var createdTask = await _repository.CreateAsync(task);

        // Broadcast a real-time message via Redis Pub/Sub
        // This could be picked up by a WebSockets/SignalR hub to update a frontend UI
        await _cache.PublishAsync("task_updates", $"New Task Created: {createdTask.Title}");

        return Ok(createdTask); // RabbitMQ will be handled by the OutboxProcessor automatically!
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        string cacheKey = $"task_{id}";

        // 1. Try to get from Cache (Redis)
        var cachedTask = await _cache.GetAsync<TaskItem>(cacheKey);
        if (cachedTask != null)
        {
            Console.WriteLine("--> Cache Hit! Returning from Redis.");
            return Ok(cachedTask);
        }

        // 2. Cache Miss - Get from Database (Postgres)
        Console.WriteLine("--> Cache Miss! Going to Postgres.");
        var task = await _repository.GetById(id); // Use DbContext directly or Repository

        if (task == null) return NotFound();

        // 3. Save to Cache for future requests (Cache-Aside)
        await _cache.SetAsync(cacheKey, task, TimeSpan.FromMinutes(10));

        return Ok(task);
    }
}