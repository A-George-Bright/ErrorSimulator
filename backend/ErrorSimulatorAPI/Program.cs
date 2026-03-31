using ErrorSimulatorAPI.Interfaces;
using ErrorSimulatorAPI.Models;
using ErrorSimulatorAPI.Services;
using ErrorSimulatorAPI.Simulation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// 🔥 FIXED LOG PATH (ABSOLUTE)
var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");

// Ensure folder exists
Directory.CreateDirectory(logFolder);

// Debug: print path
Console.WriteLine($"🔥 Logs will be saved at: {logFolder}");

// 🔥 SERILOG CONFIGURATION
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(logFolder, "app-.log"),
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}"
    )
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔥 SERVICES
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<FailureSimulator>();
builder.Services.AddSingleton<SimulationState>();

builder.Services.AddScoped<ITransferService, TransferService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("TestDb"));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 🔥 SEED DATA
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Users.AddRange(
        new User { Id = 1, Balance = 1000000 },
        new User { Id = 2, Balance = 10000}
    );

    db.SaveChanges();

    Log.Information("Database seeded with initial users");
}

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// 🔥 TEST LOG (FORCE FILE CREATION)
Log.Information("🔥 Application started successfully");
Log.Information("🔥 Log system initialized and working");

app.Run();