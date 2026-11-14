using System.Net;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Photino.Okf_Todo.Bridge;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
var run = Path.Combine(root, "artifacts", "promotion-demo");
var db = Path.Combine(run, "data", "promotion-demo.db");
if (!File.Exists(db)) throw new InvalidOperationException("Run commands.py discover to initialize the demo database first.");
var preferencesPath = Path.Combine(run, "demo-preferences.json");
if (!File.Exists(preferencesPath))
{
    File.Copy(Path.Combine(root, "tools", "promotion-demo", "demo-preferences.json"), preferencesPath);
}
var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(DatabasePathProvider.CreateConnectionString(db, pooling: false)));
builder.Services.AddSingleton<HtmlSanitizerService>();
builder.Services.AddSingleton(new McpClientConfigurationService(run));
builder.Services.AddSingleton(sp => new HelpRuntimeContextService(root, db, sp.GetRequiredService<McpClientConfigurationService>()));
builder.Services.AddSingleton<IAppPreferencePathProvider>(new DemoPreferences(preferencesPath));
builder.Services.AddSingleton<IBackupDestinationPicker, DisabledBackup>();
builder.Services.AddSingleton<ITaskMarkdownExportDestinationPicker, DisabledExport>();
builder.Services.AddSingleton<IExternalLinkLauncher, DisabledExternalLinks>();
builder.Services.AddSingleton<ExternalLinkService>();
builder.Services.AddScoped<LookupSeedService>();
builder.Services.AddScoped<TaskLifecycleService>();
builder.Services.AddScoped<TaskListService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TaskAttachmentService>();
builder.Services.AddScoped<TaskChecklistService>();
builder.Services.AddScoped<TaskRelationService>();
builder.Services.AddScoped<AppPreferenceService>();
builder.Services.AddScoped<IssueService>();
builder.Services.AddScoped<ImageService>();
builder.Services.AddScoped<DatabaseBackupService>();
builder.Services.AddScoped<TaskMarkdownExportService>();
builder.Services.AddScoped<SampleDataSeeder>();
builder.Services.AddScoped<SampleDataService>();
builder.Services.AddSingleton<ApplicationCommandService>();
builder.Services.AddSingleton<BridgeMessageHandler>();
var app = builder.Build();
app.UseStaticFiles(new StaticFileOptions {FileProvider = new PhysicalFileProvider(Path.Combine(run, "capture")), RequestPath = "/recording"});
app.UseStaticFiles(new StaticFileOptions {FileProvider = new PhysicalFileProvider(Path.Combine(root,"docs","help")), RequestPath = "/help"});
app.UseStaticFiles(new StaticFileOptions {FileProvider = new PhysicalFileProvider(Path.Combine(root,"Okf-Todo","wwwroot"))});
app.MapPost("/__ui-test/bridge", async (HttpRequest request, BridgeMessageHandler handler, CancellationToken ct) => {
    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var message = await reader.ReadToEndAsync(ct);
    var response = await handler.HandleAsync(message, ct);
    return Results.Text(response, "application/json", Encoding.UTF8);
});
await app.StartAsync();
var url = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
await File.WriteAllTextAsync(Path.Combine(run, "browser-url.txt"), url);
app.Logger.LogInformation("Demo frontend at {Url}; isolated database {DatabasePath}", url, db);
await app.WaitForShutdownAsync();

sealed class DemoPreferences(string path) : IAppPreferencePathProvider { public string GetPreferencesPath() => path; }
sealed class DisabledBackup : IBackupDestinationPicker {
    public Task<string?> PickAsync(string suggestedFileName, string? initialDirectory, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
sealed class DisabledExport : ITaskMarkdownExportDestinationPicker {
    public Task<string?> PickAsync(string suggestedFileName, string? initialDirectory, CancellationToken cancellationToken) => Task.FromResult<string?>(null);
}
sealed class DisabledExternalLinks : IExternalLinkLauncher { public void Open(Uri uri) { } }


