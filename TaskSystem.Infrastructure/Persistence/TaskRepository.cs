using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskSystem.Application.Abstractions;
using TaskSystem.Domain.Entities;

namespace TaskSystem.Infrastructure.Persistence;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem> CreateAsync(TaskItem task)
    {
        // Start a transaction
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 1. Add the Task
            _context.Tasks.Add(task);

            // 2. Add the Outbox Message
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredOnUtc = DateTime.UtcNow,
                Type = "TaskCreatedEvent",
                Content = JsonSerializer.Serialize(new { task.Id, task.Title, task.CreatedAt })
            };
            _context.OutboxMessages.Add(outboxMessage);

            // 3. Save both
            await _context.SaveChangesAsync();

            // 4. Commit the transaction
            await transaction.CommitAsync();

            return task;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<IEnumerable<TaskItem>> GetAllAsync()
    {
        return await _context.Tasks.ToListAsync();
    }

    public async Task<TaskItem?> GetById(Guid id)
    {
        return await _context.Tasks.FindAsync(id);
    }
}