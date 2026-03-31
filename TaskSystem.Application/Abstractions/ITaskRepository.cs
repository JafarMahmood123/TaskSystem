using TaskSystem.Domain.Entities;
namespace TaskSystem.Application.Abstractions;

public interface ITaskRepository
{
    Task<TaskItem> CreateAsync(TaskItem task);
    Task<IEnumerable<TaskItem>> GetAllAsync();
    Task<TaskItem?> GetById(Guid id);
}