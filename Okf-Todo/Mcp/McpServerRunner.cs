using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ModelContextProtocol.Server;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Photino.Okf_Todo.Mcp;

internal static class McpServerRunner
{
    public static async Task RunAsync(string databasePath)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddConsole(options =>
        {
            // The MCP transport owns stdout. All application and framework logs use stderr.
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath)));
        builder.Services.AddScoped<LookupSeedService>();
        builder.Services.AddScoped<TaskLifecycleService>();
        builder.Services.AddScoped<TaskListService>();
        builder.Services.AddScoped<TaskService>();
        builder.Services.AddSingleton<ApplicationCommandService>();
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        using var host = builder.Build();

        await using (var scope = host.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await dbContext.Database.MigrateAsync();
            await scope.ServiceProvider.GetRequiredService<LookupSeedService>().SeedAsync();
            await scope.ServiceProvider.GetRequiredService<TaskListService>().EnsureDefaultListAsync();
        }

        host.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("OkfTodoMcp")
            .LogInformation("OKF-Todo MCP server is using database {DatabasePath}.", databasePath);

        await host.RunAsync();
    }
}
