using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Infrastructure.Data;
using TaskManager.Infrastructure.Identity;
using TaskManager.Infrastructure.Repositories;

namespace TaskManager.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer. Wires up the EF Core context and repositories.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<TaskDbContext>(options =>
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                // Fallback used by quick local runs and tests when no SQL Server is configured.
                options.UseInMemoryDatabase("TaskManagerDb");
            }
            else
            {
                options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
            }
        });

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        return services;
    }
}
