using Microsoft.EntityFrameworkCore;
using MontrealFoodViolations.Application.Interfaces;
using MontrealFoodViolations.Application.Options;
using MontrealFoodViolations.Infrastructure.Data;
using MontrealFoodViolations.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ViolationSyncOptions>(builder.Configuration.GetSection("ViolationSync"));
builder.Services.Configure<MontrealDatasetOptions>(builder.Configuration.GetSection("MontrealDataset"));

builder.Services.AddDbContext<MontrealFoodViolationsDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=montrealfoodviolations.db"));

builder.Services.AddHttpClient("MontrealDataset", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MontrealFoodViolations/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddSingleton<IDatasetSyncCoordinator, DatasetSyncCoordinator>();
builder.Services.AddScoped<IMontrealDatasetClient, MontrealDatasetClient>();
builder.Services.AddScoped<IViolationSyncService, ViolationSyncService>();
builder.Services.AddHostedService<ViolationSyncBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MontrealFoodViolationsDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
