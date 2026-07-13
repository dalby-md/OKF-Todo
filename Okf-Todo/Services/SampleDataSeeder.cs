using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Photino.Okf_Todo.Data;

namespace Photino.Okf_Todo.Services;

public sealed class SampleDataSeeder(
    AppDbContext dbContext,
    TaskService taskService,
    TaskChecklistService checklistService,
    TaskAttachmentService attachmentService,
    TaskRelationService relationService,
    ImageService imageService,
    ILogger<SampleDataSeeder> logger)
{
    public const string SampleTag = "sample-data";
    private const string ImagePlaceholder = "{{SAMPLE_IMAGE}}";
    private const string SamplePngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=";

    private static readonly string[] Titles =
    [
        "Fix failed production deployment",
        "Investigate intermittent API timeout",
        "Review urgent security patch",
        "Replace expired integration certificate",
        "Diagnose nightly import failure",
        "Prepare Power Platform release notes",
        "Follow up on ServiceDesk incident",
        "Optimize slow Oracle APEX report",
        "Validate database backup restore",
        "Update local development onboarding",
        "Resolve Azure DevOps pipeline warning",
        "Investigate memory growth in worker",
        "Add retry handling to email processor",
        "Document emergency rollback procedure",
        "Review monitoring alert thresholds",
        "Clean up obsolete feature flags",
        "Reproduce customer login issue",
        "Improve task editor keyboard workflow",
        "Verify deployment variable replacement",
        "Assess SQLite database size growth",
        "Add diagnostics for blank Photino window",
        "Review dependency update impact",
        "Investigate duplicate support notifications",
        "Prepare release readiness checklist",
        "Fix incorrect overdue task sorting",
        "Validate clipboard image paste",
        "Test drag and drop image handling",
        "Review HTML sanitization allowlist",
        "Improve attachment size validation",
        "Analyze slow startup trace",
        "Complete ServiceDesk root cause summary",
        "Archive resolved deployment notes",
        "Finalize quarterly maintenance checklist",
        "Complete Power Platform connector review",
        "Close obsolete monitoring investigation",
        "Document fixed Oracle APEX session issue",
        "Verify restored database integrity",
        "Complete editor image regression test",
        "Close duplicate Azure DevOps request",
        "Finish release retrospective notes",
        "Complete local backup validation",
        "Resolve checklist ordering defect",
        "Cancel superseded UI redesign",
        "Cancel duplicate certificate request",
        "Cancel obsolete integration spike",
        "Cancel deferred cloud sync research",
        "Cancel replaced deployment approach",
        "Cancel duplicate monitoring task",
        "Cancel outdated onboarding request",
        "Cancel unsupported export proposal"
    ];

    private static readonly string[] TaskTypeCodes =
    [
        "CRITICAL_ERROR", "ERROR", "REQUEST", "IDEA", "NOTE", "INVESTIGATION", "IMPROVEMENT"
    ];

    private static readonly string?[] PriorityCodes =
    [
        "URGENT", "NORMAL", "NORMAL", "URGENT", "CAN_WAIT", null, "NORMAL", "CAN_WAIT"
    ];

    private static readonly string[] SourceCodes =
    [
        "DEPLOYMENT", "MONITORING_LOGS", "EMAIL", "SERVICEDESK", "TFS_AZURE_DEVOPS",
        "ORACLE_APEX", "POWER_PLATFORM", "TEAMS", "USER_REPORT", "MANUAL"
    ];

    private static readonly string[][] DomainTags =
    [
        ["deployment", "backend"],
        ["monitoring", "performance"],
        ["security", "maintenance"],
        ["certificate", "integration"],
        ["database", "support"],
        ["power-platform", "release"],
        ["servicedesk", "incident"],
        ["oracle-apex", "performance"],
        ["backup", "database"],
        ["documentation", "developer-experience"]
    ];

    private static readonly IReadOnlyDictionary<int, string> WaitingTargets = new Dictionary<int, string>
    {
        [2] = "Platform team response",
        [7] = "ServiceDesk INC240107",
        [12] = "Memory dump from operations",
        [17] = "User reproduction details",
        [22] = "Dependency owner approval",
        [27] = "WebView runtime verification"
    };

    private static readonly (int Source, int Target, string Type)[] Relations =
    [
        (0, 1, "BLOCKS"),
        (2, 3, "DEPENDS_ON"),
        (4, 5, "RELATED_TO"),
        (6, 7, "CREATED_FROM"),
        (8, 9, "FOLLOW_UP_TO"),
        (10, 11, "DUPLICATE_OF"),
        (12, 13, "BLOCKS"),
        (14, 15, "DEPENDS_ON"),
        (16, 17, "RELATED_TO"),
        (18, 19, "CREATED_FROM"),
        (20, 21, "FOLLOW_UP_TO"),
        (22, 23, "DUPLICATE_OF")
    ];

    public async Task<SampleDataSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await dbContext.TaskItems.AnyAsync(
            task => task.Tags.Any(taskTag => taskTag.TaskTag != null && taskTag.TaskTag.Value == SampleTag),
            cancellationToken))
        {
            throw new InvalidOperationException(
                $"Sample tasks already exist. Remove tasks tagged '{SampleTag}' before seeding again.");
        }

        var definitions = BuildDefinitions(DateTime.UtcNow);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var taskIds = new List<int>(definitions.Count);

            foreach (var definition in definitions)
            {
                var task = await taskService.CreateAsync(new TaskSaveRequest(
                    Id: null,
                    Title: definition.Title,
                    TaskTypeCode: definition.TaskTypeCode,
                    Body: definition.Body,
                    BodyFormatCode: "HTML",
                    TaskPriorityCode: definition.PriorityCode,
                    TaskSourceCode: definition.SourceCode,
                    SourceReference: definition.SourceReference,
                    SourceUrl: definition.SourceUrl,
                    Deadline: definition.Deadline,
                    ActiveWaitingForLabel: definition.WaitingFor,
                    Tags: definition.Tags), cancellationToken);

                taskIds.Add(task.Id);
                await AddSupportingDataAsync(task, definition, cancellationToken);

                if (definition.State == SampleTaskState.Completed)
                {
                    await taskService.CompleteAsync(task.Id, cancellationToken);
                }
                else if (definition.State == SampleTaskState.Cancelled)
                {
                    await taskService.CancelAsync(task.Id, cancellationToken);
                }
            }

            foreach (var relation in Relations)
            {
                await relationService.CreateAsync(new TaskRelationCreateRequest(
                    taskIds[relation.Source],
                    taskIds[relation.Target],
                    relation.Type), cancellationToken);
            }

            await ApplyDemonstrationTimestampsAsync(taskIds, DateTime.UtcNow, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var result = new SampleDataSeedResult(
                TaskCount: taskIds.Count,
                FirstTaskId: taskIds.Min(),
                LastTaskId: taskIds.Max());
            logger.LogInformation(
                "Created {TaskCount} sample tasks with ids {FirstTaskId} through {LastTaskId}.",
                result.TaskCount,
                result.FirstTaskId,
                result.LastTaskId);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task AddSupportingDataAsync(
        TaskDetailDto task,
        SampleTaskDefinition definition,
        CancellationToken cancellationToken)
    {
        if (definition.Number <= 3 || definition.Number % 3 == 0)
        {
            await taskService.AddCommentAsync(new TaskCommentCreateRequest(
                task.Id,
                $"Initial assessment for sample {definition.Number:00}: evidence collected and next action identified."), cancellationToken);
        }

        if (definition.Number % 10 == 0)
        {
            await taskService.AddCommentAsync(new TaskCommentCreateRequest(
                task.Id,
                "Follow-up review confirmed that the acceptance criteria are still accurate."), cancellationToken);
        }

        if (definition.Number <= 2 || definition.Number % 4 == 0)
        {
            var checklist = await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(task.Id, "Reproduce or confirm the current behavior"),
                cancellationToken);
            checklist = await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(task.Id, "Implement or document the agreed change"),
                cancellationToken);
            checklist = await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(task.Id, "Verify the result and update the timeline"),
                cancellationToken);

            var ordered = checklist.OrderBy(item => item.SortOrder).ToList();
            await checklistService.SetCompletedAsync(
                new TaskChecklistCompleteRequest(task.Id, ordered[0].Id, true),
                cancellationToken);
            if (definition.Number % 8 == 0)
            {
                await checklistService.SetCompletedAsync(
                    new TaskChecklistCompleteRequest(task.Id, ordered[1].Id, true),
                    cancellationToken);
            }
        }

        if (definition.Number == 1 || definition.Number % 7 == 0)
        {
            var attachmentText = $"Sample evidence for task {definition.Number:00}\r\nTitle: {definition.Title}\r\n";
            await attachmentService.CreateAsync(new TaskAttachmentCreateRequest(
                task.Id,
                $"sample-{definition.Number:00}-evidence.txt",
                "text/plain",
                Convert.ToBase64String(Encoding.UTF8.GetBytes(attachmentText)),
                "Small text attachment stored as a SQLite BLOB."), cancellationToken);
        }

        if (definition.Number % 10 == 0)
        {
            var image = await imageService.CreateAsync(new ImageCreateRequest(
                IssueId: null,
                TaskId: task.Id,
                Filename: $"sample-{definition.Number:00}-status.png",
                MimeType: "image/png",
                Base64Data: SamplePngBase64,
                Width: 1,
                Height: 1), cancellationToken);

            await taskService.UpdateAsync(new TaskSaveRequest(
                Id: task.Id,
                Title: task.Title,
                TaskTypeCode: task.TaskTypeCode,
                Body: definition.Body.Replace(
                    ImagePlaceholder,
                    $"<img src=\"{image.Src}\" alt=\"Stored sample image\" width=\"96\" height=\"48\">",
                    StringComparison.Ordinal),
                BodyFormatCode: task.BodyFormatCode,
                TaskPriorityCode: task.TaskPriorityCode,
                TaskSourceCode: task.TaskSourceCode,
                SourceReference: task.SourceReference,
                SourceUrl: task.SourceUrl,
                Deadline: task.Deadline,
                ActiveWaitingForLabel: task.ActiveWaitingFor?.Label,
                Tags: task.Tags), cancellationToken);
        }
    }

    private async Task ApplyDemonstrationTimestampsAsync(
        IReadOnlyCollection<int> taskIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var tasks = await dbContext.TaskItems
            .AsSplitQuery()
            .Include(task => task.TaskStatus)
            .Include(task => task.LogEntries)
            .Include(task => task.Comments)
            .Include(task => task.ChecklistItems)
            .Include(task => task.Attachments)
            .Include(task => task.Images)
            .Include(task => task.WaitingTargets)
            .Where(task => taskIds.Contains(task.Id))
            .OrderBy(task => task.Id)
            .ToListAsync(cancellationToken);

        for (var index = 0; index < tasks.Count; index++)
        {
            var task = tasks[index];
            var createdAt = now.Date.AddDays(-(50 - index)).AddHours(8 + index % 8);
            task.CreatedAt = createdAt;
            task.ActivatedAt = createdAt.AddMinutes(5);

            var eventTime = createdAt.AddMinutes(15);
            foreach (var log in task.LogEntries.OrderBy(log => log.Id))
            {
                log.CreatedAt = eventTime;
                eventTime = eventTime.AddMinutes(20);
            }

            foreach (var comment in task.Comments.OrderBy(comment => comment.Id))
            {
                comment.CreatedAt = eventTime;
                eventTime = eventTime.AddMinutes(30);
            }

            foreach (var item in task.ChecklistItems.OrderBy(item => item.SortOrder))
            {
                item.CreatedAt = eventTime;
                item.UpdatedAt = eventTime;
                if (item.IsCompleted)
                {
                    item.CompletedAt = eventTime.AddMinutes(10);
                }

                eventTime = eventTime.AddMinutes(20);
            }

            foreach (var attachment in task.Attachments)
            {
                attachment.CreatedAt = eventTime;
                eventTime = eventTime.AddMinutes(10);
            }

            foreach (var image in task.Images)
            {
                image.CreatedUtc = eventTime;
                eventTime = eventTime.AddMinutes(10);
            }

            foreach (var waitingFor in task.WaitingTargets)
            {
                waitingFor.CreatedAt = createdAt.AddHours(2);
                waitingFor.WaitingSince = waitingFor.CreatedAt;
                waitingFor.UpdatedAt = waitingFor.ResolvedAt is null ? waitingFor.CreatedAt : eventTime;
                if (waitingFor.ResolvedAt is not null)
                {
                    waitingFor.ResolvedAt = eventTime;
                    eventTime = eventTime.AddMinutes(10);
                }
            }

            if (task.TaskStatus?.Code == TaskStatusCodes.Completed)
            {
                task.CompletedAt = eventTime;
                task.CancelledAt = null;
            }
            else if (task.TaskStatus?.Code == TaskStatusCodes.Cancelled)
            {
                task.CancelledAt = eventTime;
                task.CompletedAt = null;
            }

            task.UpdatedAt = eventTime;
        }

        var relations = await dbContext.TaskRelations
            .Where(relation => taskIds.Contains(relation.SourceTaskId))
            .OrderBy(relation => relation.Id)
            .ToListAsync(cancellationToken);
        for (var index = 0; index < relations.Count; index++)
        {
            relations[index].CreatedAt = now.Date.AddDays(-(12 - index)).AddHours(14);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static IReadOnlyList<SampleTaskDefinition> BuildDefinitions(DateTime now)
    {
        return Titles.Select((title, index) =>
        {
            var number = index + 1;
            var sourceCode = SourceCodes[index % SourceCodes.Length];
            var state = number >= 43
                ? SampleTaskState.Cancelled
                : number >= 31
                    ? SampleTaskState.Completed
                    : SampleTaskState.Active;
            var tags = new[] { SampleTag }
                .Concat(DomainTags[index % DomainTags.Length])
                .Concat([state.ToString().ToLowerInvariant()])
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new SampleTaskDefinition(
                Number: number,
                Title: title,
                TaskTypeCode: TaskTypeCodes[index % TaskTypeCodes.Length],
                PriorityCode: PriorityCodes[index % PriorityCodes.Length],
                SourceCode: sourceCode,
                SourceReference: BuildSourceReference(sourceCode, number),
                SourceUrl: number % 3 == 0 ? $"https://example.invalid/tasks/{number:00}" : null,
                Deadline: BuildDeadline(number, state, now),
                WaitingFor: state == SampleTaskState.Active && WaitingTargets.TryGetValue(number, out var target)
                    ? target
                    : null,
                Tags: tags,
                Body: BuildBody(number, title, sourceCode),
                State: state);
        }).ToList();
    }

    private static DateTime? BuildDeadline(int number, SampleTaskState state, DateTime now)
    {
        if (state == SampleTaskState.Cancelled || number % 6 == 0)
        {
            return null;
        }

        if (state == SampleTaskState.Completed)
        {
            return now.Date.AddDays(-(number % 9 + 1));
        }

        if (number <= 8)
        {
            return now.Date.AddDays(-(number % 4 + 1));
        }

        return now.Date.AddDays(number % 12 + 1);
    }

    private static string? BuildSourceReference(string sourceCode, int number)
    {
        return sourceCode switch
        {
            "SERVICEDESK" => $"INC24{number:0000}",
            "EMAIL" => $"Subject: follow-up {number:00}",
            "TEAMS" => $"Developer Support thread {number:00}",
            "TFS_AZURE_DEVOPS" => $"Work item #{1800 + number}",
            "ORACLE_APEX" => $"APP 145 / page {number}",
            "POWER_PLATFORM" => $"Solution release 2026.{number:00}",
            "DEPLOYMENT" => $"Release #{1700 + number}",
            "MONITORING_LOGS" => $"Alert OKF-{number:000}",
            "USER_REPORT" => $"Report #{500 + number}",
            _ => null
        };
    }

    private static string BuildBody(int number, string title, string sourceCode)
    {
        var details = (number % 5) switch
        {
            0 => "<table><thead><tr><th>Environment</th><th>Result</th></tr></thead><tbody><tr><td>Development</td><td>Verified</td></tr><tr><td>Production</td><td>Pending</td></tr></tbody></table>",
            1 => "<ul><li>Collect evidence</li><li>Confirm scope</li><li>Verify the result</li></ul>",
            2 => "<blockquote>Keep the change local, observable, and easy to reverse.</blockquote>",
            3 => "<ol><li>Reproduce</li><li>Implement</li><li>Validate</li></ol>",
            _ => "<pre><code>sample diagnostic output\nstatus: review required</code></pre>"
        };
        var image = number % 10 == 0 ? $"<p>{ImagePlaceholder}</p>" : string.Empty;

        return $"""
            <h2>{title}</h2>
            <p>This sample task demonstrates rich HTML content for a <strong>{sourceCode}</strong> work item.</p>
            {details}
            {image}
            <p><a href="https://example.invalid/reference/{number:00}">Related reference</a></p>
            """;
    }

    private sealed record SampleTaskDefinition(
        int Number,
        string Title,
        string TaskTypeCode,
        string? PriorityCode,
        string SourceCode,
        string? SourceReference,
        string? SourceUrl,
        DateTime? Deadline,
        string? WaitingFor,
        IReadOnlyCollection<string> Tags,
        string Body,
        SampleTaskState State);

    private enum SampleTaskState
    {
        Active,
        Completed,
        Cancelled
    }
}

public sealed record SampleDataSeedResult(int TaskCount, int FirstTaskId, int LastTaskId);
