using Microsoft.EntityFrameworkCore;
using RegistreAlimentaire.Application.Interfaces;
using RegistreAlimentaire.Application.Options;
using RegistreAlimentaire.Infrastructure.Data;
using RegistreAlimentaire.Infrastructure.Services;
using Swashbuckle.AspNetCore.SwaggerUI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ViolationSyncOptions>(builder.Configuration.GetSection("ViolationSync"));
builder.Services.Configure<MontrealDatasetOptions>(builder.Configuration.GetSection("MontrealDataset"));

builder.Services.AddDbContext<RegistreAlimentaireDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=registrealimentaire.db"));

builder.Services.AddHttpClient("MontrealDataset", client =>
{
    client.DefaultRequestHeaders.UserAgent.ParseAdd("RegistreAlimentaire/1.0");
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddSingleton<IDatasetSyncCoordinator, DatasetSyncCoordinator>();
builder.Services.AddScoped<IMontrealDatasetClient, MontrealDatasetClient>();
builder.Services.AddScoped<IViolationSyncService, ViolationSyncService>();
builder.Services.AddHostedService<ViolationSyncBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Title = "Registre alimentaire";
        document.Info.Version = "v1";
        document.Info.Description = "API des condamnations et amendes alimentaires de Montréal et du reste du Québec.";
        return Task.CompletedTask;
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RegistreAlimentaireDbContext>();
    db.Database.Migrate();
}

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Registre alimentaire v1");
});

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.Run();
