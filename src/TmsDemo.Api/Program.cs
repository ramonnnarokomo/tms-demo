using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TmsDemo.Api.Data;
using TmsDemo.Api.Infrastructure;
using TmsDemo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services.AddDbContext<TmsDbContext>((services, options) =>
    options.UseSqlite(services.GetRequiredService<IConfiguration>().GetConnectionString("Tms")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<ICarrierService, CarrierService>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();               // OpenAPI document at /openapi/v1.json
    app.MapScalarApiReference();    // Interactive docs at /scalar/v1
}

app.MapControllers();

// Demo shortcut: create the SQLite database on startup.
// In a real project you would use EF Core migrations instead (dotnet ef migrations add ...).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TmsDbContext>();
    db.Database.EnsureCreated();

    if (app.Environment.IsDevelopment())
        DemoDataSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<TimeProvider>());
}

app.Run();

// Makes Program visible to the integration tests (WebApplicationFactory<Program>).
public partial class Program { }
