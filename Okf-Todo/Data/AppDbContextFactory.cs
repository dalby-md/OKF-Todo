using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Photino.Okf_Todo.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "okf-todo-ef-design-time.db");
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(DatabasePathProvider.CreateConnectionString(databasePath, pooling: false))
            .Options;

        return new AppDbContext(options);
    }
}
