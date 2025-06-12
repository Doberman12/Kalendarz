using Microsoft.EntityFrameworkCore;
/// <summary>
/// Database z zadaniami
/// </summary>
public class TasksContext : DbContext
{
    public DbSet<TaskItem> Tasks { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=tasks.db");
    }
}
