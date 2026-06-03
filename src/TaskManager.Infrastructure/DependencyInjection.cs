using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Ai;
using TaskManager.Application.Ai.Interfaces;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Users.Interfaces;
using TaskManager.Infrastructure.Ai;
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
                // Fallback used by quick local runs and tests when no database is configured.
                options.UseInMemoryDatabase("TaskManagerDb");
            }
            else
            {
                options.UseSqlite(connectionString);
            }
        });

        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();

        // AI assistance (OpenAI) — bound from the "OpenAI" config section and exposed as a typed
        // HttpClient so the Application layer depends only on IAiTaskAssistant.
        services.Configure<OpenAiSettings>(configuration.GetSection(OpenAiSettings.SectionName));
        services.AddHttpClient<IAiTaskAssistant, OpenAiTaskAssistant>();

        return services;
    }
}
