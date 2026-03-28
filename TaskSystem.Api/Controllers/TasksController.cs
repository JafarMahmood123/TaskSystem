using Microsoft.AspNetCore.Mvc;
using TaskSystem.Application.Abstractions;
using TaskSystem.Domain.Entities;

namespace TaskSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskRepository _repository;
    private readonly ICacheService _cache;
    private const string TasksCacheKey = "all_tasks_key";

    public TasksController(ITaskRepository repository, ICacheService cache)
    {
        _repository = repository;
        _cache = cache;
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
        var createdTask = await _repository.CreateAsync(task);
        
        // Invalidate Cache 
        await _cache.RemoveAsync(TasksCacheKey);

        return Ok(createdTask);
    }
}