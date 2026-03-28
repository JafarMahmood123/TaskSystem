using Microsoft.EntityFrameworkCore;
using TaskSystem.Domain.Entities;

namespace TaskSystem.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<TaskItem> Tasks { get; set; }
}