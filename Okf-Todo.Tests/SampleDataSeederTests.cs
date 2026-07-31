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
        var taskListService = new TaskListService(dbContext);
        await taskListService.EnsureDefaultListAsync();
        var taskService = new TaskService(dbContext, lifecycleService, taskListService);
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
        Assert.Equal(23, await dbContext.TaskItems.CountAsync(task => task.ChecklistItems.Count != 0));
        Assert.Equal(117, await dbContext.TaskChecklistItems.CountAsync());
        Assert.Equal(29, await dbContext.TaskAttachments.CountAsync());
        Assert.Equal(40, await dbContext.TaskComments.CountAsync());
        Assert.Equal(5, await dbContext.Images.CountAsync(image => image.TaskId != null));
        Assert.Equal(12, await dbContext.TaskRelations.CountAsync());
        Assert.Equal(393, await dbContext.TaskLogEntries.CountAsync());
        Assert.True(await dbContext.TaskTypes.AllAsync(type => type.Tasks.Count != 0));
        Assert.True(await dbContext.TaskTags.AnyAsync(tag => tag.Value == SampleDataSeeder.SampleTag));
        Assert.Equal(50, await dbContext.TaskItems.CountAsync(task => task.IsSampleData));
        Assert.True(await dbContext.TaskAttachments.AllAsync(attachment =>
            attachment.FileSize > 0
            && attachment.ContentBlob.Length == attachment.FileSize
            && attachment.Sha256Hash != null
            && attachment.Sha256Hash.Length == 64));
        Assert.True(await dbContext.TaskAttachments.SumAsync(attachment => attachment.FileSize) < 1024 * 1024);

        var deploymentCase = await dbContext.TaskItems
            .AsNoTracking()
            .Include(task => task.ChecklistItems)
            .Include(task => task.Attachments)
            .Include(task => task.Comments)
            .Include(task => task.LogEntries)
                .ThenInclude(log => log.TaskLogType)
            .SingleAsync(task => task.Title == "Fix failed production deployment");
        Assert.Equal(7, deploymentCase.ChecklistItems.Count);
        Assert.Equal(4, deploymentCase.ChecklistItems.Count(item => item.IsCompleted));
        Assert.Equal(
            ["deployment-error.log", "rollback-plan.md", "variable-diff.json"],
            deploymentCase.Attachments.Select(attachment => attachment.FileName).Order());
        Assert.Equal(4, deploymentCase.Comments.Count);
        Assert.Contains(deploymentCase.LogEntries, log =>
            log.TaskLogType!.Code == "ATTACHMENT_REMOVED"
            && log.Message.Contains("preliminary-diagnosis.txt", StringComparison.Ordinal));
        Assert.Contains(deploymentCase.LogEntries, log =>
            log.TaskLogType!.Code == TaskLogTypeCodes.PriorityChanged);
        Assert.Contains(deploymentCase.LogEntries, log =>
            log.TaskLogType!.Code == TaskLogTypeCodes.WaitingForCleared);

        var orderingCase = await dbContext.TaskItems
            .AsNoTracking()
            .Include(task => task.ChecklistItems)
            .Include(task => task.LogEntries)
                .ThenInclude(log => log.TaskLogType)
            .SingleAsync(task => task.Title == "Fix incorrect overdue task sorting");
        Assert.Equal(7, orderingCase.ChecklistItems.Count);
        Assert.Equal(2, orderingCase.ChecklistItems.Count(item => item.IsCompleted));
        Assert.Contains(orderingCase.LogEntries, log =>
            log.TaskLogType!.Code == "CHECKLIST_ITEM_REOPENED"
            && log.Message.Contains("local-date conversion", StringComparison.Ordinal));

        var rootCauseCase = await dbContext.TaskItems
            .AsNoTracking()
            .Include(task => task.LogEntries)
                .ThenInclude(log => log.TaskLogType)
            .SingleAsync(task => task.Title == "Complete ServiceDesk root cause summary");
        Assert.Equal(2, rootCauseCase.LogEntries.Count(log =>
            log.TaskLogType!.Code == TaskLogTypeCodes.TaskCompleted));
        Assert.Single(rootCauseCase.LogEntries, log =>
            log.TaskLogType!.Code == TaskLogTypeCodes.TaskReopened);

        var orderedLogTimes = deploymentCase.LogEntries
            .OrderBy(log => log.Id)
            .Select(log => log.CreatedAt)
            .ToList();
        Assert.Equal(orderedLogTimes.Order(), orderedLogTimes);

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
        Assert.Equal(50, await dbContext.TaskItems.CountAsync());

        var representativeSample = await dbContext.TaskItems.AsNoTracking().FirstAsync();
        var personalTask = new TaskItem
        {
            TaskListId = representativeSample.TaskListId,
            Title = "Personal task",
            BodyFormatId = representativeSample.BodyFormatId,
            TaskTypeId = representativeSample.TaskTypeId,
            TaskStatusId = representativeSample.TaskStatusId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsSampleData = false
        };
        dbContext.TaskItems.Add(personalTask);
        await dbContext.SaveChangesAsync();
        var sampleTag = await dbContext.TaskTags.SingleAsync(
            tag => tag.Value == SampleDataSeeder.SampleTag);
        dbContext.Set<TaskTaskTag>().Add(new TaskTaskTag
        {
            TaskId = personalTask.Id,
            TaskTagId = sampleTag.Id
        });
        await dbContext.SaveChangesAsync();

        var sampleDataService = new SampleDataService(
            dbContext,
            seeder,
            NullLogger<SampleDataService>.Instance);
        var removal = await sampleDataService.RemoveAsync();

        Assert.Equal(50, removal.RemovedTaskCount);
        Assert.Equal(1, await dbContext.TaskItems.CountAsync());
        Assert.Equal(
            "Personal task",
            await dbContext.TaskItems.Select(task => task.Title).SingleAsync());
        Assert.Equal(0, await dbContext.TaskChecklistItems.CountAsync());
        Assert.Equal(0, await dbContext.TaskAttachments.CountAsync());
        Assert.Equal(0, await dbContext.TaskComments.CountAsync());
        Assert.Equal(0, await dbContext.TaskLogEntries.CountAsync());
        Assert.Equal(0, await dbContext.TaskRelations.CountAsync());
        Assert.Equal(0, await dbContext.Images.CountAsync());
    }

    private static Task<int> CountTasksWithStatusAsync(AppDbContext dbContext, string statusCode)
    {
        return dbContext.TaskItems.CountAsync(
            task => task.TaskStatus != null && task.TaskStatus.Code == statusCode);
    }
}
