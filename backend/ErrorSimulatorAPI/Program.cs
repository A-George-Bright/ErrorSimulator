using ErrorSimulatorAPI.Interfaces;
using ErrorSimulatorAPI.Services;
using ErrorSimulatorAPI.Simulation;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 🔥 LOG PATH
var logFolder = Path.Combine(Directory.GetCurrentDirectory(), "logs");
Directory.CreateDirectory(logFolder);
Console.WriteLine($"🔥 Logs will be saved at: {logFolder}");

// 🔥 SERILOG
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 🔥 SERVICES
builder.Services.AddSingleton<SimulationService>();
builder.Services.AddSingleton<FailureSimulator>();
builder.Services.AddSingleton<SimulationState>();

builder.Services.AddScoped<ITransferService, TransferService>();

// 🗄️ MYSQL (Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 45))));

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

// 🔄 AUTO-MIGRATE ON STARTUP
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrated successfully");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("🔥 Application started successfully");
Log.Information("🔥 Log system initialized and working");

app.Run();
