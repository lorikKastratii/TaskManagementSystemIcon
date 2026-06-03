using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.Infrastructure.Data;

/// <summary>
/// Design-time factory used by the EF Core tools (`dotnet ef migrations add`). It targets the
/// SQLite provider with a placeholder data source so migrations are always generated for the
/// production database, independent of whatever provider the running app resolves.
/// </summary>
public class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseSqlite("Data Source=taskmanager.db")
            .Options;

        return new TaskDbContext(options);
    }
}
