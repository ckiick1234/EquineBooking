using Azure.Identity;
using Azure.Messaging.ServiceBus;
using EquineBooking.Core.Interfaces;
using EquineBooking.Infrastructure.Data;
using EquineBooking.Infrastructure.Options;
using EquineBooking.Infrastructure.Repositories;
using EquineBooking.Infrastructure.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;
using SendGrid;

namespace EquineBooking.Infrastructure;

/// <summary>
/// DI registration helpers for the EquineBooking infrastructure layer.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Register Cosmos DB plumbing, repositories, the availability service, the SendGrid
    /// notification service, and the Service Bus publisher. Configuration is sourced from
    /// the <c>CosmosDb</c>, <c>SendGrid</c>, <c>Admin</c>, <c>Facility</c>, <c>Venmo</c>,
    /// and <c>ServiceBus</c> sections (env vars use <c>__</c> separators).
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        AddCosmos(services, configuration);
        AddNotifications(services, configuration);
        AddServiceBus(services, configuration);

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<ISpaceRepository, SpaceRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();

        return services;
    }

    /// <summary>
    /// Same as <see cref="AddInfrastructure"/>, but routes notifications to
    /// <see cref="LocalDevNotificationService"/> (logs instead of sending) when
    /// <paramref name="environment"/> is Development.
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        AddInfrastructure(services, configuration);

        if (environment.IsDevelopment())
        {
            services.AddScoped<INotificationService, LocalDevNotificationService>();
        }

        return services;
    }

    private static void AddCosmos(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CosmosOptions.SectionName);
        var cosmosOptions = new CosmosOptions
        {
            Endpoint = section["Endpoint"] ?? string.Empty,
            ConnectionString = section["ConnectionString"] ?? string.Empty,
            DatabaseName = section["DatabaseName"] ?? string.Empty
        };
        services.AddSingleton(MsOptions.Create(cosmosOptions));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            var clientOptions = new CosmosClientOptions
            {
                Serializer = new CosmosSystemTextJsonSerializer(),
                ConnectionMode = ConnectionMode.Direct
            };

            if (!string.IsNullOrWhiteSpace(options.Endpoint))
            {
                return new CosmosClient(options.Endpoint, new DefaultAzureCredential(), clientOptions);
            }

            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return new CosmosClient(options.ConnectionString, clientOptions);
            }

            throw new InvalidOperationException($"Configuration value '{CosmosOptions.SectionName}:Endpoint' or '{CosmosOptions.SectionName}:ConnectionString' is required.");
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
            if (string.IsNullOrWhiteSpace(options.DatabaseName))
            {
                throw new InvalidOperationException($"Configuration value '{CosmosOptions.SectionName}:DatabaseName' is required.");
            }
            var client = sp.GetRequiredService<CosmosClient>();
            return new CosmosDbContext(client, options.DatabaseName);
        });
    }

    private static void AddNotifications(IServiceCollection services, IConfiguration configuration)
    {
        var sendGridSection = configuration.GetSection(SendGridOptions.SectionName);
        var sendGridOptions = new SendGridOptions
        {
            ApiKey = sendGridSection["ApiKey"] ?? string.Empty,
            FromEmail = sendGridSection["FromEmail"] ?? string.Empty,
            FromName = sendGridSection["FromName"] ?? string.Empty
        };
        services.AddSingleton(MsOptions.Create(sendGridOptions));

        var notificationOptions = new NotificationOptions
        {
            AdminEmail = configuration["Admin:Email"] ?? string.Empty,
            FacilityName = configuration["Facility:Name"] ?? "Equine Facility",
            AdminBaseUrl = configuration["Admin:BaseUrl"] ?? string.Empty,
            VenmoUrl = configuration["Venmo:Url"] ?? string.Empty
        };
        services.AddSingleton(MsOptions.Create(notificationOptions));

        services.AddSingleton<ISendGridClient>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<SendGridOptions>>().Value;
            return new SendGridClient(opts.ApiKey);
        });

        services.AddScoped<INotificationService, SendGridNotificationService>();
    }

    private static void AddServiceBus(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ServiceBusOptions.SectionName);
        var serviceBusOptions = new ServiceBusOptions
        {
            FullyQualifiedNamespace = section["FullyQualifiedNamespace"] ?? string.Empty,
            ConnectionString = section["ConnectionString"] ?? string.Empty
        };
        services.AddSingleton(MsOptions.Create(serviceBusOptions));

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<ServiceBusOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
            {
                return new ServiceBusClient(options.FullyQualifiedNamespace, new DefaultAzureCredential());
            }

            if (!string.IsNullOrWhiteSpace(options.ConnectionString))
            {
                return new ServiceBusClient(options.ConnectionString);
            }

            throw new InvalidOperationException($"Configuration value '{ServiceBusOptions.SectionName}:FullyQualifiedNamespace' or '{ServiceBusOptions.SectionName}:ConnectionString' is required.");
        });

        services.AddSingleton<ServiceBusPublisher>();
    }
}
