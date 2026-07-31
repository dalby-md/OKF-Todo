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

    private static readonly HashSet<int> WorkedCaseNumbers = [1, 2, 3, 4, 5, 8, 9, 21, 25, 31];

    private static readonly IReadOnlyDictionary<int, string[]> WorkedCaseChecklists =
        new Dictionary<int, string[]>
        {
            [1] =
            [
                "Capture the failed production deployment output",
                "Compare production variables with the staging configuration",
                "Identify the missing secret reference",
                "Validate the rollback package",
                "Deploy the corrected configuration",
                "Run production smoke tests",
                "Update the incident record with the outcome"
            ],
            [2] =
            [
                "Confirm the affected API route and time window",
                "Correlate gateway timings with worker traces",
                "Reproduce the timeout under controlled load",
                "Test the connection-pool hypothesis",
                "Verify the fix against the recorded request pattern",
                "Document monitoring signals for recurrence"
            ],
            [3] =
            [
                "Read the vendor advisory and affected-version matrix",
                "Confirm the deployed package version",
                "Prepare the patched build in an isolated environment",
                "Run authentication and authorization regression checks",
                "Review the change with the security owner",
                "Schedule the production maintenance window",
                "Record final verification evidence"
            ],
            [4] =
            [
                "Inventory services using the expiring certificate",
                "Request the replacement certificate",
                "Validate the complete certificate chain",
                "Install the certificate in the test environment",
                "Rotate production bindings",
                "Confirm expiry monitoring detects the new date"
            ],
            [5] =
            [
                "Preserve the failed import payload",
                "Locate the first rejected record",
                "Compare the payload with the current mapping",
                "Correct the transformation rule",
                "Replay the import in the test environment",
                "Verify downstream record totals"
            ],
            [8] =
            [
                "Capture the report SQL and current execution plan",
                "Measure the slowest filter combination",
                "Identify the highest-cost operation",
                "Test the candidate index in development",
                "Compare before-and-after timings",
                "Document the production rollout and rollback steps"
            ],
            [9] =
            [
                "Create a fresh encrypted database backup",
                "Restore the backup into an isolated location",
                "Run SQLite integrity_check",
                "Compare task and attachment counts",
                "Open representative rich-content tasks",
                "Verify attachment hashes after restore",
                "Record the recovery duration"
            ],
            [21] =
            [
                "Confirm the local static server answers the readiness probe",
                "Capture the WebView navigation sequence",
                "Compare a successful and blank-window startup",
                "Verify the cache-busting URL is requested",
                "Add diagnostic context to startup logging",
                "Retest packaged and source-checkout builds"
            ],
            [25] =
            [
                "Write boundary cases for yesterday, today, and tomorrow",
                "Reproduce ordering with urgent and waiting tasks",
                "Confirm local-date conversion at midnight",
                "Implement the smallest sorting correction",
                "Run service-level ordering tests",
                "Verify Attention grouping in the desktop UI",
                "Update the sorting guidance"
            ],
            [31] =
            [
                "Consolidate the incident timeline",
                "Confirm the triggering condition",
                "Document contributing factors",
                "Record the immediate remediation",
                "Define preventive follow-up actions",
                "Review the summary with operations",
                "Publish the final ServiceDesk response"
            ]
        };

    private static readonly IReadOnlyDictionary<int, int> WorkedCaseCompletedChecklistCounts =
        new Dictionary<int, int>
        {
            [1] = 4,
            [2] = 2,
            [3] = 5,
            [4] = 3,
            [5] = 2,
            [8] = 3,
            [9] = 6,
            [21] = 2,
            [25] = 3,
            [31] = 7
        };

    private static readonly IReadOnlyDictionary<int, string[]> WorkedCaseComments =
        new Dictionary<int, string[]>
        {
            [1] =
            [
                "The deployment fails only when the production variable group is applied. Staging remains healthy.",
                "The missing secret reference is confirmed. Rollback has been validated and the corrected package is ready.",
                "Production smoke tests passed for sign-in, task creation, and attachment download."
            ],
            [2] =
            [
                "Timeouts cluster around requests that open a second database connection after the initial query.",
                "Operations supplied a trace from the affected window; the worker queue remained below its alert threshold.",
                "The connection-pool adjustment removed the spike in the controlled load test."
            ],
            [3] =
            [
                "The deployed version is affected, but the vulnerable endpoint is restricted to authenticated administrators.",
                "Regression checks passed in the isolated environment. Awaiting approval for the maintenance window."
            ],
            [4] =
            [
                "Two services share the binding. Both must be rotated in the same maintenance window.",
                "The replacement chain validates successfully in test; production binding changes are prepared."
            ],
            [5] =
            [
                "The first rejected record contains a newly introduced empty country code.",
                "The adjusted mapping imports the preserved payload without changing valid records."
            ],
            [8] =
            [
                "The report is fast for a single department but degrades sharply when All departments is selected.",
                "The candidate index reduced the representative execution from 18.4 seconds to 1.7 seconds."
            ],
            [9] =
            [
                "Restore completed in an isolated directory; the source backup was not modified.",
                "Integrity check returned ok and all sampled attachment hashes match."
            ],
            [21] =
            [
                "The static readiness probe succeeds, but the blank run never requests the timestamped index URL.",
                "Adding navigation-stage logging confirms the hang occurs before the frontend bridge initializes."
            ],
            [25] =
            [
                "A task due today is incorrectly grouped with yesterday when the test runs near the UTC boundary.",
                "The service ordering is correct after using the local date boundary; desktop grouping still needs verification."
            ],
            [31] =
            [
                "The root cause was an outdated transformation rule deployed with the previous mapping package.",
                "Operations reviewed the corrective actions and accepted the monitoring follow-up.",
                "The final response has been added to the ServiceDesk incident."
            ]
        };

    private static readonly IReadOnlyDictionary<int, string> TemporaryResolvedWaitingTargets =
        new Dictionary<int, string>
        {
            [1] = "Release manager approval",
            [9] = "Recovery test window from operations",
            [21] = "WebView runtime trace"
        };

    private static readonly IReadOnlyDictionary<int, string> WorkedCasePriorityOverrides =
        new Dictionary<int, string>
        {
            [1] = "URGENT",
            [2] = "NORMAL",
            [3] = "URGENT",
            [4] = "URGENT",
            [5] = "NORMAL",
            [8] = "NORMAL",
            [9] = "NORMAL",
            [21] = "NORMAL",
            [25] = "URGENT",
            [31] = "NORMAL"
        };

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
                var isWorkedCase = WorkedCaseNumbers.Contains(definition.Number);
                var initialPriorityCode = isWorkedCase && definition.PriorityCode == TaskPriorityCodes.Urgent
                    ? "NORMAL"
                    : definition.PriorityCode;
                var task = await taskService.CreateAsync(new TaskSaveRequest(
                    Id: null,
                    Title: definition.Title,
                    TaskTypeCode: definition.TaskTypeCode,
                    Body: definition.Body,
                    BodyFormatCode: "HTML",
                    TaskPriorityCode: initialPriorityCode,
                    TaskSourceCode: definition.SourceCode,
                    SourceReference: definition.SourceReference,
                    SourceUrl: definition.SourceUrl,
                    Deadline: isWorkedCase ? null : definition.Deadline,
                    ActiveWaitingForLabel: isWorkedCase ? null : definition.WaitingFor,
                    Tags: definition.Tags), cancellationToken);

                taskIds.Add(task.Id);
                if (isWorkedCase)
                {
                    task = await taskService.UpdateAsync(new TaskSaveRequest(
                        Id: task.Id,
                        Title: task.Title,
                        TaskTypeCode: task.TaskTypeCode,
                        Body: task.Body,
                        BodyFormatCode: task.BodyFormatCode,
                        TaskPriorityCode: definition.PriorityCode,
                        TaskSourceCode: task.TaskSourceCode,
                        SourceReference: task.SourceReference,
                        SourceUrl: task.SourceUrl,
                        Deadline: definition.Deadline,
                        ActiveWaitingForLabel: definition.WaitingFor,
                        Tags: task.Tags), cancellationToken);
                }

                await AddSupportingDataAsync(task, definition, cancellationToken);

                if (definition.State == SampleTaskState.Completed)
                {
                    await taskService.CompleteAsync(task.Id, cancellationToken);
                    if (definition.Number == 31)
                    {
                        await taskService.ReopenAsync(task.Id, cancellationToken);
                        await taskService.AddCommentAsync(new TaskCommentCreateRequest(
                            task.Id,
                            "The incident owner requested one clarification to the preventive-action section before closure."), cancellationToken);
                        await taskService.CompleteAsync(task.Id, cancellationToken);
                    }
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

            await dbContext.TaskItems
                .Where(task => taskIds.Contains(task.Id))
                .ExecuteUpdateAsync(
                    updates => updates.SetProperty(task => task.IsSampleData, true),
                    cancellationToken);
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
        foreach (var comment in BuildComments(definition))
        {
            await taskService.AddCommentAsync(new TaskCommentCreateRequest(
                task.Id,
                comment), cancellationToken);
        }

        var checklistTexts = BuildChecklist(definition);
        IReadOnlyCollection<TaskChecklistItemDto> checklist = [];
        foreach (var text in checklistTexts)
        {
            checklist = await checklistService.CreateAsync(
                new TaskChecklistCreateRequest(task.Id, text),
                cancellationToken);
        }

        if (checklist.Count > 0)
        {
            var ordered = checklist.OrderBy(item => item.SortOrder).ToList();
            var completedCount = GetCompletedChecklistCount(definition, ordered.Count);
            for (var index = 0; index < completedCount; index++)
            {
                await checklistService.SetCompletedAsync(
                    new TaskChecklistCompleteRequest(task.Id, ordered[index].Id, true),
                    cancellationToken);
            }

            if (definition.Number == 25)
            {
                await checklistService.SetCompletedAsync(
                    new TaskChecklistCompleteRequest(task.Id, ordered[2].Id, false),
                    cancellationToken);
            }
        }

        foreach (var attachment in BuildAttachments(definition))
        {
            var attachments = await attachmentService.CreateAsync(new TaskAttachmentCreateRequest(
                task.Id,
                attachment.FileName,
                attachment.ContentType,
                Convert.ToBase64String(Encoding.UTF8.GetBytes(attachment.Content)),
                attachment.Description), cancellationToken);

            if (attachment.RemoveAfterAdding)
            {
                var added = attachments.Single(item => item.FileName == attachment.FileName);
                await attachmentService.DeleteAsync(
                    new TaskAttachmentDeleteRequest(task.Id, added.Id),
                    cancellationToken);
            }
        }

        if (TemporaryResolvedWaitingTargets.TryGetValue(definition.Number, out var temporaryWaitingTarget))
        {
            await taskService.AddWaitingForAsync(
                new TaskWaitingForSaveRequest(task.Id, temporaryWaitingTarget),
                cancellationToken);
            await taskService.AddCommentAsync(new TaskCommentCreateRequest(
                task.Id,
                $"Received {temporaryWaitingTarget.ToLowerInvariant()}; work can continue."), cancellationToken);
            await taskService.ClearWaitingForAsync(task.Id, cancellationToken);
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

    private static IReadOnlyCollection<string> BuildComments(SampleTaskDefinition definition)
    {
        if (WorkedCaseComments.TryGetValue(definition.Number, out var comments))
        {
            return comments;
        }

        if (definition.Number % 4 != 0 && definition.Number % 9 != 0)
        {
            return [];
        }

        return definition.State switch
        {
            SampleTaskState.Completed =>
            ["Verification is complete and the result has been recorded for future reference."],
            SampleTaskState.Cancelled =>
            ["The original approach has been superseded. Existing notes are retained for context."],
            _ => definition.SourceCode switch
            {
                "DEPLOYMENT" => ["The release output has been preserved and the rollback path is confirmed."],
                "MONITORING_LOGS" => ["The alert window is correlated with application and infrastructure metrics."],
                "SERVICEDESK" => ["The reported behavior has been confirmed and the next update is prepared."],
                "ORACLE_APEX" => ["Development timings are captured; production validation remains outstanding."],
                "POWER_PLATFORM" => ["Solution Checker findings have been reviewed and assigned."],
                _ => ["The available evidence has been reviewed and the next concrete action is identified."]
            }
        };
    }

    private static IReadOnlyCollection<string> BuildChecklist(SampleTaskDefinition definition)
    {
        if (WorkedCaseChecklists.TryGetValue(definition.Number, out var workedCaseChecklist))
        {
            return workedCaseChecklist;
        }

        if (definition.Number % 4 != 0 && definition.Number % 9 != 0)
        {
            return [];
        }

        return definition.SourceCode switch
        {
            "DEPLOYMENT" =>
            [
                "Capture the release output",
                "Validate environment-specific variables",
                "Confirm the rollback path",
                "Run post-deployment smoke checks"
            ],
            "MONITORING_LOGS" =>
            [
                "Define the affected time window",
                "Correlate application and infrastructure metrics",
                "Test the leading hypothesis",
                "Record the monitoring follow-up"
            ],
            "EMAIL" or "SERVICEDESK" or "USER_REPORT" =>
            [
                "Confirm the reported behavior",
                "Collect the minimum reproduction evidence",
                "Prepare the proposed resolution",
                "Verify the outcome with the requester"
            ],
            "ORACLE_APEX" =>
            [
                "Capture the affected page and session context",
                "Reproduce the behavior in development",
                "Validate the proposed application change",
                "Record production verification"
            ],
            "POWER_PLATFORM" =>
            [
                "Export the current solution details",
                "Review connector and environment dependencies",
                "Validate the change in a test environment",
                "Record the release result"
            ],
            _ =>
            [
                "Confirm scope and expected outcome",
                "Collect supporting evidence",
                "Complete the agreed change",
                "Verify and document the result"
            ]
        };
    }

    private static int GetCompletedChecklistCount(SampleTaskDefinition definition, int checklistCount)
    {
        if (WorkedCaseCompletedChecklistCounts.TryGetValue(definition.Number, out var workedCaseCount))
        {
            return Math.Min(workedCaseCount, checklistCount);
        }

        return definition.State switch
        {
            SampleTaskState.Completed => checklistCount,
            SampleTaskState.Cancelled => Math.Min(1, checklistCount),
            _ => Math.Min(1 + definition.Number % 2, checklistCount)
        };
    }

    private static IReadOnlyCollection<SampleAttachmentDefinition> BuildAttachments(
        SampleTaskDefinition definition)
    {
        return definition.Number switch
        {
            1 =>
            [
                TextAttachment("deployment-error.log", "text/plain", """
                    2026-07-30T18:42:11Z INFO  Applying production variable group
                    2026-07-30T18:42:12Z ERROR Secret reference 'TodoSigningKey' was not resolved
                    2026-07-30T18:42:12Z INFO  Deployment stopped before application restart
                    """, "Sanitized deployment output from the failed release."),
                TextAttachment("variable-diff.json", "application/json", """
                    {
                      "environment": "production",
                      "missingReferences": ["TodoSigningKey"],
                      "changedValues": ["HealthCheckUrl"]
                    }
                    """, "Sanitized comparison of staging and production variables."),
                TextAttachment("rollback-plan.md", "text/markdown", """
                    # Rollback plan

                    1. Stop the pending deployment.
                    2. Restore release 1700.
                    3. Verify sign-in and task creation.
                    4. Notify the incident channel.
                    """, "Reviewed rollback steps for the release."),
                TextAttachment("preliminary-diagnosis.txt", "text/plain",
                    "Initial theory: package corruption. Superseded after variable comparison.",
                    "An early diagnosis retained only in Timeline after removal.", removeAfterAdding: true)
            ],
            2 =>
            [
                TextAttachment("timeout-trace.log", "text/plain", """
                    request=4f55 route=/api/tasks duration_ms=15102 status=504
                    worker_queue_depth=3 db_connections_active=24
                    second_connection_wait_ms=14911
                    """, "Sanitized trace from an affected API request."),
                TextAttachment("response-times.csv", "text/csv", """
                    test,requests,p50_ms,p95_ms,timeouts
                    baseline,500,118,14892,17
                    adjusted-pool,500,104,386,0
                    """, "Controlled load-test comparison." )
            ],
            3 =>
            [
                TextAttachment("security-advisory-notes.md", "text/markdown", """
                    # Advisory review

                    - Deployed version is affected.
                    - Exposure requires authenticated administrator access.
                    - Patched build is available for validation.
                    """, "Internal review of the fictional vendor advisory."),
                TextAttachment("patch-validation.csv", "text/csv", """
                    check,result
                    authentication,passed
                    authorization,passed
                    attachment-download,passed
                    """, "Regression results for the patched build.")
            ],
            4 =>
            [
                TextAttachment("certificate-inventory.csv", "text/csv", """
                    service,binding,expires
                    inbound-api,api.example.invalid,2026-08-03
                    email-worker,smtp.example.invalid,2026-08-03
                    """, "Fictional certificate usage inventory."),
                TextAttachment("rotation-plan.md", "text/markdown", """
                    # Rotation sequence

                    Rotate test bindings first, validate the complete chain, then rotate both production services in one window.
                    """, "Coordinated certificate rotation plan.")
            ],
            5 =>
            [
                TextAttachment("rejected-record.json", "application/json", """
                    { "recordId": "EXAMPLE-1042", "countryCode": "", "status": "rejected" }
                    """, "Sanitized record that reproduces the mapping failure."),
                TextAttachment("import-totals.csv", "text/csv", """
                    run,accepted,rejected
                    failed-nightly,1842,1
                    corrected-replay,1843,0
                    """, "Before-and-after import totals.")
            ],
            8 =>
            [
                TextAttachment("explain-plan.txt", "text/plain", """
                    PLAN HASH VALUE: 420018
                    1 SELECT STATEMENT  COST 18422
                    2 HASH JOIN         ROWS 481230
                    3 TABLE ACCESS FULL SUPPORT_CASES
                    """, "Sanitized representative Oracle execution plan."),
                TextAttachment("before-after-timings.csv", "text/csv", """
                    scenario,before_ms,after_ms
                    single-department,820,610
                    all-departments,18400,1700
                    """, "Measured report timings in development.")
            ],
            9 =>
            [
                TextAttachment("restore-output.log", "text/plain", """
                    backup_open=ok
                    restore_copy=ok
                    migrate=ok
                    integrity_check=ok
                    """, "Output from the isolated recovery exercise."),
                TextAttachment("integrity-check.txt", "text/plain", """
                    SQLite integrity_check: ok
                    Tasks checked: 50
                    Attachment hashes sampled: 8/8 matched
                    """, "Database and attachment verification summary.")
            ],
            12 => [TextAttachment("worker-memory-samples.csv", "text/csv",
                "minute,working_set_mb\n0,184\n15,211\n30,247\n45,286\n",
                "Sanitized worker-memory samples.")],
            18 => [TextAttachment("keyboard-test-matrix.csv", "text/csv",
                "context,key,expected\nworkspace,F8,save\neditor,F3,focus search\ndialog,F8,save visible dialog\n",
                "Keyboard workflow verification matrix.")],
            21 =>
            [
                TextAttachment("startup-sequence.log", "text/plain", """
                    static-server ready: 200 /index.html
                    PhotinoWindow.Load index.html?v=1722360000
                    navigation request: not observed
                    frontend bridge: not initialized
                    """, "Sanitized startup trace from a blank-window run."),
                TextAttachment("successful-startup.log", "text/plain", """
                    static-server ready: 200 /index.html
                    navigation request: 200 /index.html?v=1722360300
                    frontend bridge: initialized
                    """, "Comparison trace from a successful startup.")
            ],
            24 => [TextAttachment("release-readiness.md", "text/markdown",
                "# Release readiness\n\n- Database backup verified\n- Rollback owner assigned\n- Smoke-test scope agreed\n",
                "Release readiness notes.")],
            25 =>
            [
                TextAttachment("date-boundary-cases.csv", "text/csv", """
                    deadline,expected
                    yesterday,overdue
                    today,not overdue
                    tomorrow,not overdue
                    """, "Expected overdue behavior at the local-date boundary."),
                TextAttachment("ordering-results.txt", "text/plain", """
                    urgent+overdue -> first group
                    overdue only  -> second group
                    urgent only   -> third group
                    duplicate rows -> none
                    """, "Attention-view ordering verification.")
            ],
            26 => [TextAttachment("capture-metadata.json", "application/json",
                "{ \"source\": \"clipboard\", \"format\": \"image/png\", \"sanitized\": true }",
                "Metadata used for clipboard-image validation.")],
            29 => [TextAttachment("attachment-limits.md", "text/markdown",
                "# Attachment validation\n\nMaximum file size: 25 MB. Duplicate content is identified by SHA-256 hash.\n",
                "Attachment validation acceptance notes.")],
            31 =>
            [
                TextAttachment("incident-timeline.csv", "text/csv", """
                    time,event
                    01:10,nightly import started
                    01:18,first record rejected
                    07:35,operations escalated incident
                    10:20,corrected replay completed
                    """, "Condensed fictional incident timeline."),
                TextAttachment("root-cause-summary.md", "text/markdown", """
                    # Root cause

                    An outdated transformation rule rejected a newly valid empty country code.

                    ## Prevention

                    Add mapping-contract tests and alert on rejected-record count.
                    """, "Reviewed root-cause summary.")
            ],
            33 => [TextAttachment("maintenance-checklist.md", "text/markdown",
                "# Quarterly maintenance\n\n- Verify backups\n- Review certificates\n- Remove obsolete flags\n",
                "Completed quarterly maintenance record.")],
            37 => [TextAttachment("restored-counts.csv", "text/csv",
                "entity,expected,actual\ntasks,50,50\nattachments,29,29\nrelationships,12,12\n",
                "Representative restored-database count comparison.")],
            41 => [TextAttachment("backup-validation.txt", "text/plain",
                "Backup opened successfully. Integrity check: ok. Representative attachments downloaded successfully.",
                "Final local backup validation result.")],
            _ => []
        };
    }

    private static SampleAttachmentDefinition TextAttachment(
        string fileName,
        string contentType,
        string content,
        string description,
        bool removeAfterAdding = false)
    {
        return new SampleAttachmentDefinition(
            fileName,
            contentType,
            content.Trim() + Environment.NewLine,
            description,
            removeAfterAdding);
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
                .ThenInclude(log => log.TaskLogType)
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

            var comments = task.Comments.OrderBy(comment => comment.Id).ToList();
            var checklistItems = task.ChecklistItems.OrderBy(item => item.SortOrder).ToList();
            var attachments = task.Attachments.OrderBy(attachment => attachment.Id).ToList();
            var timestampedChecklistIds = new HashSet<int>();
            var timestampedAttachmentIds = new HashSet<int>();
            var commentIndex = 0;
            var eventTime = createdAt;
            foreach (var log in task.LogEntries.OrderBy(log => log.Id))
            {
                log.CreatedAt = eventTime;
                var logCode = log.TaskLogType?.Code;
                switch (logCode)
                {
                    case TaskLogTypeCodes.CommentAdded when commentIndex < comments.Count:
                        comments[commentIndex].CreatedAt = eventTime;
                        commentIndex++;
                        break;
                    case "CHECKLIST_ITEM_ADDED":
                    {
                        var item = checklistItems.FirstOrDefault(candidate =>
                            !timestampedChecklistIds.Contains(candidate.Id)
                            && string.Equals(candidate.Text, log.NewValue, StringComparison.Ordinal));
                        if (item is not null)
                        {
                            item.CreatedAt = eventTime;
                            item.UpdatedAt = eventTime;
                            timestampedChecklistIds.Add(item.Id);
                        }

                        break;
                    }
                    case "CHECKLIST_ITEM_COMPLETED":
                    case "CHECKLIST_ITEM_REOPENED":
                    {
                        var itemText = logCode == "CHECKLIST_ITEM_COMPLETED" ? log.NewValue : log.OldValue;
                        var item = checklistItems.FirstOrDefault(candidate =>
                            string.Equals(candidate.Text, itemText, StringComparison.Ordinal));
                        if (item is not null)
                        {
                            item.UpdatedAt = eventTime;
                            item.CompletedAt = logCode == "CHECKLIST_ITEM_COMPLETED" ? eventTime : null;
                        }

                        break;
                    }
                    case "ATTACHMENT_ADDED":
                    {
                        var attachment = attachments.FirstOrDefault(candidate =>
                            !timestampedAttachmentIds.Contains(candidate.Id)
                            && string.Equals(candidate.FileName, log.NewValue, StringComparison.Ordinal));
                        if (attachment is not null)
                        {
                            attachment.CreatedAt = eventTime;
                            timestampedAttachmentIds.Add(attachment.Id);
                        }

                        break;
                    }
                    case TaskLogTypeCodes.TaskCompleted:
                        task.CompletedAt = eventTime;
                        task.CancelledAt = null;
                        break;
                    case TaskLogTypeCodes.TaskReopened:
                        task.CompletedAt = null;
                        task.CancelledAt = null;
                        break;
                    case TaskLogTypeCodes.TaskCancelled:
                        task.CancelledAt = eventTime;
                        task.CompletedAt = null;
                        break;
                }

                eventTime = eventTime.AddMinutes(35 + task.Id % 4 * 5);
            }

            foreach (var comment in comments.Skip(commentIndex))
            {
                comment.CreatedAt = eventTime;
                eventTime = eventTime.AddMinutes(30);
            }

            foreach (var item in checklistItems.Where(item => !timestampedChecklistIds.Contains(item.Id)))
            {
                item.CreatedAt = eventTime;
                item.UpdatedAt = eventTime;
                eventTime = eventTime.AddMinutes(20);
            }

            foreach (var attachment in attachments.Where(
                         attachment => !timestampedAttachmentIds.Contains(attachment.Id)))
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
                task.CompletedAt ??= eventTime;
                task.CancelledAt = null;
            }
            else if (task.TaskStatus?.Code == TaskStatusCodes.Cancelled)
            {
                task.CancelledAt ??= eventTime;
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
            var priorityCode = WorkedCasePriorityOverrides.TryGetValue(number, out var workedCasePriority)
                ? workedCasePriority
                : PriorityCodes[index % PriorityCodes.Length];
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
                PriorityCode: priorityCode,
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
        var workedCaseBody = number switch
        {
            1 => """
                <p><strong>Impact:</strong> The production deployment stopped before the application restart. The previous release remains online.</p>
                <h3>Current finding</h3>
                <p>The production variable group references a signing secret that is not present in the target environment. Staging uses a different reference and is unaffected.</p>
                <h3>Exit criteria</h3>
                <ul><li>Correct the secret reference.</li><li>Validate rollback.</li><li>Pass production smoke tests.</li></ul>
                """,
            2 => """
                <p><strong>Symptom:</strong> A small percentage of task API requests reach the gateway timeout while worker CPU and queue depth remain normal.</p>
                <h3>Leading hypothesis</h3>
                <p>A second database connection waits for capacity after the initial query. The attached trace and load-test results capture the pattern.</p>
                """,
            3 => """
                <p><strong>Reason for urgency:</strong> A vendor advisory affects the deployed package version. Exploitation requires authenticated administrator access, but the patch should be applied promptly.</p>
                <blockquote>Use the normal maintenance path; do not bypass regression checks because the change is urgent.</blockquote>
                """,
            4 => """
                <p>Two integrations share a certificate that expires soon. The replacement must be validated and both production bindings rotated in the same maintenance window.</p>
                <table><thead><tr><th>Service</th><th>Environment</th><th>State</th></tr></thead><tbody><tr><td>Inbound API</td><td>Test</td><td>Validated</td></tr><tr><td>Email worker</td><td>Production</td><td>Pending</td></tr></tbody></table>
                """,
            5 => """
                <p>The nightly import rejected one record after a source-system mapping change. Valid records completed successfully.</p>
                <pre><code>record: EXAMPLE-1042
                field: countryCode
                value: ""
                result: rejected by legacy rule</code></pre>
                """,
            8 => """
                <p>The Oracle APEX support report becomes slow when <strong>All departments</strong> is selected. A single-department run remains responsive.</p>
                <table><thead><tr><th>Scenario</th><th>Before</th><th>Candidate index</th></tr></thead><tbody><tr><td>Single department</td><td>0.82 s</td><td>0.61 s</td></tr><tr><td>All departments</td><td>18.4 s</td><td>1.7 s</td></tr></tbody></table>
                """,
            9 => """
                <p>Run a complete recovery exercise using a new backup and an isolated restore location. The source backup must remain unchanged.</p>
                <h3>Validation scope</h3>
                <ul><li>SQLite integrity check</li><li>Task and attachment counts</li><li>Representative rich-content tasks</li><li>Attachment hashes</li></ul>
                """,
            21 => """
                <p>The local static server answers its readiness probe, but an intermittent blank run never requests the timestamped index URL.</p>
                <h3>Diagnostic boundary</h3>
                <ol><li>Static server readiness</li><li>Photino Load call</li><li>WebView navigation request</li><li>Frontend bridge initialization</li></ol>
                """,
            25 => """
                <p>Overdue ordering is incorrect close to the UTC date boundary. Product behavior is based on the current local date: yesterday is overdue, today is not.</p>
                <h3>Required ordering</h3>
                <ol><li>Urgent and overdue</li><li>Overdue</li><li>Urgent</li></ol>
                """,
            31 => """
                <p>This root-cause summary closes the ServiceDesk incident created by the failed nightly import.</p>
                <h3>Root cause</h3>
                <p>An outdated transformation rule rejected a newly valid empty country code.</p>
                <h3>Prevention</h3>
                <ul><li>Add mapping-contract tests.</li><li>Alert on rejected-record count.</li><li>Review source-schema changes before deployment.</li></ul>
                """,
            _ => null
        };
        var details = (number % 5) switch
        {
            0 => "<table><thead><tr><th>Environment</th><th>Result</th></tr></thead><tbody><tr><td>Development</td><td>Verified</td></tr><tr><td>Production</td><td>Pending</td></tr></tbody></table>",
            1 => "<ul><li>Collect evidence</li><li>Confirm scope</li><li>Verify the result</li></ul>",
            2 => "<blockquote>Keep the change local, observable, and easy to reverse.</blockquote>",
            3 => "<ol><li>Reproduce</li><li>Implement</li><li>Validate</li></ol>",
            _ => "<pre><code>sample diagnostic output\nstatus: review required</code></pre>"
        };
        var image = number % 10 == 0 ? $"<p>{ImagePlaceholder}</p>" : string.Empty;
        var content = workedCaseBody ?? $"""
            <p><strong>Context:</strong> This work originated from {sourceCode} and has been captured with enough detail for the next review.</p>
            {details}
            <p><strong>Expected outcome:</strong> Resolve or document the issue and leave a verifiable result.</p>
            """;

        return $"""
            <h2>{title}</h2>
            {content}
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

    private sealed record SampleAttachmentDefinition(
        string FileName,
        string ContentType,
        string Content,
        string Description,
        bool RemoveAfterAdding);

    private enum SampleTaskState
    {
        Active,
        Completed,
        Cancelled
    }
}

public sealed record SampleDataSeedResult(int TaskCount, int FirstTaskId, int LastTaskId);
