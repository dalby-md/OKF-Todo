using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class SampleDataService(
    AppDbContext dbContext,
    SampleDataSeeder sampleDataSeeder,
    ILogger<SampleDataService> logger)
{
    public async Task<DatabaseDataStatusDto> GetStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return new DatabaseDataStatusDto(
            TotalTaskCount: await dbContext.TaskItems.CountAsync(cancellationToken),
            SampleTaskCount: await dbContext.TaskItems.CountAsync(
                task => task.IsSampleData,
                cancellationToken),
            TrashTaskCount: await dbContext.TaskItems.CountAsync(
                task => task.DeletedAt != null,
                cancellationToken),
            AttachmentCount: await dbContext.TaskAttachments.CountAsync(cancellationToken),
            CommentCount: await dbContext.TaskComments.CountAsync(cancellationToken),
            ChecklistItemCount: await dbContext.TaskChecklistItems.CountAsync(cancellationToken),
            RelationshipCount: await dbContext.TaskRelations.CountAsync(cancellationToken),
            TaskListCount: await dbContext.TaskLists.CountAsync(cancellationToken));
    }

    public async Task<SampleDataSeedResult> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.TaskItems.AnyAsync(cancellationToken))
        {
            throw new ValidationException(
                "Sample data can only be added when the database has no tasks.",
                "database");
        }

        return await sampleDataSeeder.SeedAsync(cancellationToken);
    }

    public async Task<SampleDataRemovalResult> RemoveAsync(
        CancellationToken cancellationToken = default)
    {
        var sampleTaskIds = await dbContext.TaskItems
            .Where(task => task.IsSampleData)
            .Select(task => task.Id)
            .ToListAsync(cancellationToken);
        if (sampleTaskIds.Count == 0)
        {
            return new SampleDataRemovalResult(0);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var relations = await dbContext.TaskRelations
                .Where(relation => sampleTaskIds.Contains(relation.SourceTaskId)
                    || sampleTaskIds.Contains(relation.TargetTaskId))
                .ToListAsync(cancellationToken);
            var tasks = await dbContext.TaskItems
                .Where(task => sampleTaskIds.Contains(task.Id))
                .ToListAsync(cancellationToken);

            dbContext.TaskRelations.RemoveRange(relations);
            dbContext.TaskItems.RemoveRange(tasks);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Removed {SampleTaskCount} sample tasks and their owned data.",
                sampleTaskIds.Count);
            return new SampleDataRemovalResult(sampleTaskIds.Count);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public sealed record DatabaseDataStatusDto(
    int TotalTaskCount,
    int SampleTaskCount,
    int TrashTaskCount,
    int AttachmentCount,
    int CommentCount,
    int ChecklistItemCount,
    int RelationshipCount,
    int TaskListCount);

public sealed record SampleDataRemovalResult(int RemovedTaskCount);
