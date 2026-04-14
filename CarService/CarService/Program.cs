using System.Text.Json.Serialization;
using CarRentalService.Data;
using CarRentalService.Data.Repositories;
using CarService.CurrencyConverterService;
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

namespace CarService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        const string FrontendCorsPolicy = "FrontendCors";

        builder.Services.AddHttpClient();

        builder.Services.AddDbContext<CarRentalDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlOptions => sqlOptions.MigrationsAssembly("CarRentalService.Web")));

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
                options.AddPolicy("User", builder =>
                {
                    builder
                        .RequireRealmRoles("app-user"); // Realm role is fetched from token
                });
                options.AddPolicy("Admin", builder =>
                {
                    builder
                        .RequireRealmRoles("app-admin"); // Realm role is fetched from token
                });
            })
            .AddKeycloakAuthorization(builder.Configuration);
        var options =
            builder.Configuration.GetSection("KeycloakAdmin").Get<KeycloakAdminClientOptions>();
        if (options is null)
        {
            throw new InvalidOperationException("Missing required configuration section: KeycloakAdmin");
        }

        builder.Services.AddDistributedMemoryCache();
        var hasKeycloakAdminClientCredentials =
            !string.IsNullOrWhiteSpace(options.Resource) &&
            !string.IsNullOrWhiteSpace(options.KeycloakTokenEndpoint) &&
            options.Credentials is not null &&
            !string.IsNullOrWhiteSpace(options.Credentials.Secret);

        if (hasKeycloakAdminClientCredentials)
        {
            builder.Services
                .AddClientCredentialsTokenManagement()
                .AddClient(
                    "keycloak-admin-token",
                    client =>
                    {
                        client.ClientId = ClientId.Parse(options.Resource);
                        client.ClientSecret = ClientSecret.Parse(options.Credentials.Secret);
                        client.TokenEndpoint = new Uri(options.KeycloakTokenEndpoint);
                    }
                );

            var tokenClientName = ClientCredentialsClientName.Parse("keycloak-admin-token");
            builder.Services.AddKeycloakAdminHttpClient(options)
                .AddClientCredentialsTokenHandler(tokenClientName);
        }
        else
        {
            builder.Services.AddKeycloakAdminHttpClient(options);
            Console.WriteLine("Warning: KeycloakAdmin client credentials are incomplete. SDK admin client token flow is disabled; bootstrap admin fallback will be used.");
        }

        var currencyConverterSettings =
            builder.Configuration.GetSection("CurrencyConverterSettings").Get<CurrencyConverterSettings>();
        if (currencyConverterSettings is null ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.BaseUrl) ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.Username) ||
            string.IsNullOrWhiteSpace(currencyConverterSettings.Password))
        {
            throw new InvalidOperationException("CurrencyConverterSettings is incomplete. BaseUrl, Username and Password are required.");
        }

        builder.Services.AddScoped<CurrencyConverterPortTypeClient>(provider =>
        {
            var address = new System.ServiceModel.EndpointAddress(currencyConverterSettings.BaseUrl);
            var securityMode = string.Equals(address.Uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? System.ServiceModel.BasicHttpSecurityMode.Transport
                : System.ServiceModel.BasicHttpSecurityMode.TransportCredentialOnly;

            var binding = new System.ServiceModel.BasicHttpBinding(securityMode)
            {
                Security =
                {
                    Transport = { ClientCredentialType = System.ServiceModel.HttpClientCredentialType.Basic },
                },
                MaxBufferSize = int.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                ReaderQuotas = System.Xml.XmlDictionaryReaderQuotas.Max,
                AllowCookies = true,
            };

            var client = new CurrencyConverterPortTypeClient(binding, address);
            client.ClientCredentials.UserName.UserName = currencyConverterSettings.Username;
            client.ClientCredentials.UserName.Password = currencyConverterSettings.Password;
            return client;
        });

        builder.Services.AddScoped<ICarRepository, CarRepository>();
        builder.Services.AddScoped<IBookingRepository, BookingRepository>();
        builder.Services.AddScoped<ICarService, Services.CarService>();
        builder.Services.AddScoped<IExtCurrencyConvertService, ExtCurrencyConvertService>();

        builder.Services.AddAutoMapper(cfg => { }, typeof(Program));

        builder.Services.AddExceptionHandler<GlobalExceptionHandlerMiddleware>();
        builder.Services.AddProblemDetails();

        builder.Services.AddControllers()
            .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy(FrontendCorsPolicy, policy =>
            {
                policy.WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1",
                new OpenApiInfo
                {
                    Title = "Car Rental Service API",
                    Version = "v1",
                    Description = "API Documentation"
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
            app.Services.CreateScope().ServiceProvider.GetRequiredService<CarRentalDbContext>().Database.Migrate();
        }

        app.UseExceptionHandler();
        app.UseCors(FrontendCorsPolicy);

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Car Rental Service API V1"); });
        }

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.Run();
    }
}