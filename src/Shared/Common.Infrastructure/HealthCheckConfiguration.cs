using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace Common.Infrastructure.HealthChecks;

public static class HealthCheckConfiguration
{
    public static IServiceCollection AddCustomHealthChecks(
        this IServiceCollection services,
        string connectionString,
        bool includeRedis = false,
        string? redisConnectionString = null)
    {
        var healthChecksBuilder = services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "PostgreSQL",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "postgresql" });

        if (includeRedis && !string.IsNullOrEmpty(redisConnectionString))
        {
            healthChecksBuilder.AddRedis(
                redisConnectionString,
                name: "Redis",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "cache", "redis" });
        }

        return services;
    }

    public static WebApplication MapCustomHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var response = new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(x => new
                    {
                        name = x.Key,
                        status = x.Value.Status.ToString(),
                        description = x.Value.Description,
                        duration = x.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        return app;
    }
}