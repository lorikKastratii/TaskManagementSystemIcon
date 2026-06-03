using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Application.Auth.Interfaces;
using TaskManager.Application.Auth.Services;
using TaskManager.Application.Tasks.Interfaces;
using TaskManager.Application.Tasks.Services;

namespace TaskManager.Application;

/// <summary>
/// Composition root for the Application layer. Registers use-case services and all
/// FluentValidation validators discovered in this assembly.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITokenService, TokenService>();

        // Discover and register every AbstractValidator in this assembly.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Scoped);

        return services;
    }
}
