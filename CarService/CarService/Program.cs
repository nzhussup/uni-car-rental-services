using System.Text.Json.Serialization;
using CarRentalService.Data;
using CarRentalService.Data.Repositories;
using CarService.Middleware;
using CarService.Models.Settings;
using CarService.Services;
using Duende.AccessTokenManagement;
using Keycloak.AuthServices.Authentication;
using Keycloak.AuthServices.Authorization;
using Keycloak.AuthServices.Sdk;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using CurrencyConverterClient = CurrencyConverter.Grpc.CurrencyConverter.CurrencyConverterClient;

namespace CarService;

public class Program
{
    public static void Main(string[] args)
    {
        BuildApp(args).Run();
    }

    public static WebApplication BuildApp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = Directory.GetCurrentDirectory()
        });
        const string FrontendCorsPolicy = "FrontendCors";

        builder.Services.AddHttpClient();

        builder.Services.AddDbContext<CarServiceDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly(typeof(CarServiceDbContext).Assembly.GetName().Name)));

        builder.Services.AddKeycloakWebApiAuthentication(builder.Configuration);
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidateAudience = false;
            options.TokenValidationParameters.ValidateIssuer = false;
            options.TokenValidationParameters.ValidIssuers = new[]
            {
                "http://localhost:7070/realms/car-rental-dev",
                "http://host.docker.internal:7070/realms/car-rental-dev"
            };
        });
        builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("User", policyBuilder =>
                {
                    policyBuilder.RequireRealmRoles("app-user");
                });

                options.AddPolicy("Admin", policyBuilder =>
                {
                    policyBuilder.RequireRealmRoles("app-admin");
                });
            })
            .AddKeycloakAuthorization(builder.Configuration);

        var keycloakAdminOptions =
            builder.Configuration.GetSection("KeycloakAdmin").Get<KeycloakAdminClientOptions>();

        if (keycloakAdminOptions is null)
        {
            throw new InvalidOperationException("Missing required configuration section: KeycloakAdmin");
        }

        builder.Services.AddDistributedMemoryCache();
        var hasKeycloakAdminClientCredentials =
            !string.IsNullOrWhiteSpace(keycloakAdminOptions.Resource) &&
            !string.IsNullOrWhiteSpace(keycloakAdminOptions.KeycloakTokenEndpoint) &&
            keycloakAdminOptions.Credentials is not null &&
            !string.IsNullOrWhiteSpace(keycloakAdminOptions.Credentials.Secret);

        if (hasKeycloakAdminClientCredentials)
        {
            builder.Services
                .AddClientCredentialsTokenManagement()
                .AddClient(
                    "keycloak-admin-token",
                    client =>
                    {
                        client.ClientId = ClientId.Parse(keycloakAdminOptions.Resource);
                        client.ClientSecret = ClientSecret.Parse(keycloakAdminOptions.Credentials.Secret);
                        client.TokenEndpoint = new Uri(keycloakAdminOptions.KeycloakTokenEndpoint);
                    }
                );

            var tokenClientName = ClientCredentialsClientName.Parse("keycloak-admin-token");

            builder.Services
                .AddKeycloakAdminHttpClient(keycloakAdminOptions)
                .AddClientCredentialsTokenHandler(tokenClientName);
        }
        else
        {
            builder.Services.AddKeycloakAdminHttpClient(keycloakAdminOptions);

            Console.WriteLine(
                "Warning: KeycloakAdmin client credentials are incomplete. SDK admin client token flow is disabled; bootstrap admin fallback will be used.");
        }

        var currencyConverterSettings =
            builder.Configuration.GetSection("CurrencyConverterSettings").Get<CurrencyConverterSettings>();
        if (currencyConverterSettings is null ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.GrpcUrl) ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.Username) ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.Password))
        {
            throw new InvalidOperationException(
                "CurrencyConverterSettings is incomplete. GrpcUrl, Username and Password are required.");
        }

        builder.Services.AddSingleton(currencyConverterSettings);
        builder.Services.AddGrpcClient<CurrencyConverterClient>(options =>
        {
            options.Address = new Uri(currencyConverterSettings.GrpcUrl);
        });

        builder.Services.AddSingleton(
            builder.Configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>()
            ?? throw new InvalidOperationException("Missing required configuration section: RabbitMQ"));

        builder.Services.AddScoped<IMessageProducer, MessageProducer>();
        builder.Services.AddScoped<ICarRepository, CarRepository>();
        builder.Services.AddScoped<ICarService, Services.CarService>();
        builder.Services.AddScoped<IExtCurrencyConvertService, ExtCurrencyConvertService>();
        builder.Services.AddHostedService<BookingSubscriber>();

        builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

        builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();
        builder.Services.AddProblemDetails();

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"] ?? "http://localhost:5173";
                policy.WithOrigins(allowedOrigin)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "Car Service API",
                    Version = "v1",
                    Description = "API Documentation for Car Service"
                });
        });

        var app = builder.Build();

        var skipDbMigration = app.Configuration.GetValue<bool>("SkipDbMigration")
                              || string.Equals(
                                  Environment.GetEnvironmentVariable("SKIP_DB_MIGRATION"),
                                  "true",
                                  StringComparison.OrdinalIgnoreCase);

        if (!skipDbMigration)
        {
            try
            {
                app.Services
                    .CreateScope()
                    .ServiceProvider
                    .GetRequiredService<CarServiceDbContext>()
                    .Database
                    .Migrate();
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 1801)
            {
                Console.WriteLine("Database already exists (SqlException 1801). Retrying migrations.");

                app.Services
                    .CreateScope()
                    .ServiceProvider
                    .GetRequiredService<CarServiceDbContext>()
                    .Database
                    .Migrate();
            }
        }

        app.UseExceptionHandler();
        app.UseCors(FrontendCorsPolicy);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Rental Service API V1");
            });
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();

        return app;
    }
}

public class SwaggerHostFactory
{
    public static IHost CreateHost()
    {
        Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location)!);
        return Program.BuildApp(Array.Empty<string>());
    }
}
