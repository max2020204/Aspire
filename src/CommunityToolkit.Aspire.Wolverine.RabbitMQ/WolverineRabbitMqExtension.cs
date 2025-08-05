using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.RabbitMQ;

namespace CommunityToolkit.Aspire.Wolverine.RabbitMQ;

/// <summary>
/// Provides an extension method to configure and add RabbitMQ integration with Wolverine framework
/// in a .NET application.
/// </summary>
public static class WolverineRabbitMqExtension
{
    /// <summary>
    /// Adds RabbitMQ integration with the Wolverine framework to the host application builder,
    /// allowing configuration of connection settings and optional telemetry.
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/> instance used to configure the application.</param>
    /// <param name="name">The name used for the RabbitMQ connection client.</param>
    /// <param name="telemetry">Specifies whether telemetry options, such as logging, metrics, and tracing, should be enabled.</param>
    /// <param name="connectionString">An optional connection string key from the configuration for RabbitMQ.</param>
    /// <param name="developerMode">Indicates whether RabbitMQ should run in developer mode without a connection string.</param>
    /// <param name="configure">An optional action to configure additional <see cref="WolverineOptions"/> for customization.</param>
    public static void AddWolverineRabbitMq(this IHostApplicationBuilder builder, string name, bool telemetry,
        string? connectionString = null, bool developerMode = false,
        Action<WolverineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        string? connection = "";
        builder.Services.AddWolverine(config =>
        {
            configure?.Invoke(config);
            if (developerMode)
            {
                config.UseRabbitMq();
            }
            else if (connectionString is not null && builder.Configuration[connectionString] is { } uri)
            {
                config.UseRabbitMq(new Uri(uri));
            }
            else
            {
                builder.AddKeyedRabbitMQClient(name, rabbit =>
                {
                    connection = rabbit.ConnectionString;
                });
                config.UseRabbitMq(new Uri(connection ?? string.Empty));
            }

            if (telemetry)
            {
                config.Policies.MessageExecutionLogLevel(LogLevel.Information);
                config.Policies.MessageSuccessLogLevel(LogLevel.Information);
                config.Services.AddOpenTelemetry()
                    .WithLogging()
                    .WithMetrics()
                    .WithTracing(x =>
                    {
                        x.AddSource("Wolverine");
                    });
            }
        });
    }
}