using System.Text.Json;
using System.Text.Json.Serialization;
using EquineBooking.Api.Common;
using EquineBooking.Api.Middleware;
using EquineBooking.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Web;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication(workerApp =>
    {
        workerApp.UseMiddleware<CorsMiddleware>();
        workerApp.UseMiddleware<AuthMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddInfrastructure(context.Configuration, context.HostingEnvironment);
        services.AddScoped<AuthorizationHelper>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddMicrosoftIdentityWebApi(context.Configuration.GetSection("AzureAd"));
        services.AddAuthorization();
    })
    .Build();

host.Run();
