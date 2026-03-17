using System.Text.Json.Serialization;
using CarRentalService.Data;
using CarRentalService.Data.Repositories;
using CarRentalService.Middleware;
using CarRentalService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);
const string FrontendCorsPolicy = "FrontendCors";

builder.Services.AddDbContext<CarRentalDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.MigrationsAssembly("CarRentalService.Web")));

builder.Services.AddScoped<ICarRepository, CarRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICarService, CarService>();
builder.Services.AddScoped<IBookingService, BookingService>();

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
app.MapControllers();
app.Run();