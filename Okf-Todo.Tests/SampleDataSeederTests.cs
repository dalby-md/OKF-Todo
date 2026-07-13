using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Photino.Okf_Todo.Data;
using Photino.Okf_Todo.Services;

namespace Okf_Todo.Tests;

public sealed class SampleDataSeederTests
{
    [Fact]
    public async Task SeedAsync_CreatesRepresentativeTaskSetAndRejectsDuplicateRun()
    {
        await using var connection = new SqliteConnection(
            DatabasePathProvider.CreateConnectionString(":memory:", pooling: false));
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var dbContext = new AppDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        await new LookupSeedService(dbContext, NullLogger<LookupSeedService>.Instance).SeedAsync();

        var lifecycleService = new TaskLifecycleService(
            dbContext,
            NullLogger<TaskLifecycleService>.Instance);
        var taskService = new TaskService(dbContext, lifecycleService);
        var seeder = new SampleDataSeeder(
            dbContext,
            taskService,
            new TaskChecklistService(dbContext),
            new TaskAttachmentService(dbContext),
            new TaskRelationService(dbContext),
            new ImageService(dbContext, NullLogger<ImageService>.Instance),
            NullLogger<SampleDataSeeder>.Instance);

        var result = await seeder.SeedAsync();

        Assert.Equal(50, result.TaskCount);
        Assert.Equal(50, await dbContext.TaskItems.CountAsync());
        Assert.Equal(30, await CountTasksWithStatusAsync(dbContext, TaskStatusCodes.Active));
        Assert.Equal(12, await CountTasksWithStatusAsync(dbContext, TaskStatusCodes.Completed));
        Assert.Equal(8, await CountTasksWithStatusAsync(dbContext, TaskStatusCodes.Cancelled));
        Assert.Equal(6, await dbContext.TaskWaitingFors.CountAsync(waitingFor => waitingFor.ResolvedAt == null));
        Assert.Equal(14, await dbContext.TaskItems.CountAsync(task => task.ChecklistItems.Count != 0));
        Assert.Equal(8, await dbContext.TaskAttachments.CountAsync());
        Assert.Equal(5, await dbContext.Images.CountAsync(image => image.TaskId != null));
        Assert.Equal(12, await dbContext.TaskRelations.CountAsync());
        Assert.True(await dbContext.TaskTypes.AllAsync(type => type.Tasks.Count != 0));
        Assert.True(await dbContext.TaskTags.AnyAsync(tag => tag.Value == SampleDataSeeder.SampleTag));

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
        Assert.Equal(50, await dbContext.TaskItems.CountAsync());
    }

    private static Task<int> CountTasksWithStatusAsync(AppDbContext dbContext, string statusCode)
    {
        return dbContext.TaskItems.CountAsync(
            task => task.TaskStatus != null && task.TaskStatus.Code == statusCode);
    }
}
