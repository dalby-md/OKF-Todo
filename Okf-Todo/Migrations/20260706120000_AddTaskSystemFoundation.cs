using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Photino.Okf_Todo.Data;

#nullable disable

namespace Photino.Okf_Todo.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260706120000_AddTaskSystemFoundation")]
    public partial class AddTaskSystemFoundation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            CreateLookupTable(migrationBuilder, "AttachmentKinds");
            CreateLookupTable(migrationBuilder, "BodyFormats");
            CreateLookupTable(migrationBuilder, "StakeholderRoles");
            CreateLookupTable(migrationBuilder, "StakeholderTypes");
            CreateLookupTable(migrationBuilder, "TaskLogTypes");
            CreateLookupTable(migrationBuilder, "TaskPriorities");
            CreateLookupTable(migrationBuilder, "TaskSources");
            CreateLookupTable(migrationBuilder, "TaskStatuses");
            CreateLookupTable(migrationBuilder, "TaskTypes");
            CreateLookupTable(migrationBuilder, "WaitingForTypes");

            migrationBuilder.CreateTable(
                name: "TaskRelationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReverseName = table.Column<string>(type: "TEXT", nullable: false),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRelationTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: true),
                    BodyFormatId = table.Column<int>(type: "INTEGER", nullable: true),
                    TaskTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskStatusId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskPriorityId = table.Column<int>(type: "INTEGER", nullable: true),
                    TaskSourceId = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceReference = table.Column<string>(type: "TEXT", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: true),
                    Deadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                    table.ForeignKey("FK_TaskItems_BodyFormats_BodyFormatId", x => x.BodyFormatId, "BodyFormats", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskItems_TaskPriorities_TaskPriorityId", x => x.TaskPriorityId, "TaskPriorities", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskItems_TaskSources_TaskSourceId", x => x.TaskSourceId, "TaskSources", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskItems_TaskStatuses_TaskStatusId", x => x.TaskStatusId, "TaskStatuses", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskItems_TaskTypes_TaskTypeId", x => x.TaskTypeId, "TaskTypes", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: true),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    Sha256Hash = table.Column<string>(type: "TEXT", nullable: true),
                    AttachmentKindId = table.Column<int>(type: "INTEGER", nullable: true),
                    ContentBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskAttachments", x => x.Id);
                    table.ForeignKey("FK_TaskAttachments_AttachmentKinds_AttachmentKindId", x => x.AttachmentKindId, "AttachmentKinds", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskAttachments_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskChecklistItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsCompleted = table.Column<bool>(type: "INTEGER", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskChecklistItems", x => x.Id);
                    table.ForeignKey("FK_TaskChecklistItems_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    CommentText = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskComments", x => x.Id);
                    table.ForeignKey("FK_TaskComments_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskLogTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    OldValue = table.Column<string>(type: "TEXT", nullable: true),
                    NewValue = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLogEntries", x => x.Id);
                    table.ForeignKey("FK_TaskLogEntries_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TaskLogEntries_TaskLogTypes_TaskLogTypeId", x => x.TaskLogTypeId, "TaskLogTypes", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskRelations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetTaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskRelationTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskRelations", x => x.Id);
                    table.CheckConstraint("CK_TaskRelations_SourceTarget_Different", "SourceTaskId <> TargetTaskId");
                    table.ForeignKey("FK_TaskRelations_TaskItems_SourceTaskId", x => x.SourceTaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TaskRelations_TaskItems_TargetTaskId", x => x.TargetTaskId, "TaskItems", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskRelations_TaskRelationTypes_TaskRelationTypeId", x => x.TaskRelationTypeId, "TaskRelationTypes", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskStakeholders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    StakeholderTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    StakeholderRoleId = table.Column<int>(type: "INTEGER", nullable: true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskStakeholders", x => x.Id);
                    table.ForeignKey("FK_TaskStakeholders_StakeholderRoles_StakeholderRoleId", x => x.StakeholderRoleId, "StakeholderRoles", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskStakeholders_StakeholderTypes_StakeholderTypeId", x => x.StakeholderTypeId, "StakeholderTypes", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskStakeholders_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskTaskTags",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    TaskTagId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskTaskTags", x => new { x.TaskId, x.TaskTagId });
                    table.ForeignKey("FK_TaskTaskTags_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TaskTaskTags_TaskTags_TaskTagId", x => x.TaskTagId, "TaskTags", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskWaitingFors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TaskId = table.Column<int>(type: "INTEGER", nullable: false),
                    WaitingForTypeId = table.Column<int>(type: "INTEGER", nullable: true),
                    Label = table.Column<string>(type: "TEXT", nullable: true),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Url = table.Column<string>(type: "TEXT", nullable: true),
                    StakeholderId = table.Column<int>(type: "INTEGER", nullable: true),
                    WaitingSince = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FollowUpAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskWaitingFors", x => x.Id);
                    table.ForeignKey("FK_TaskWaitingFors_TaskItems_TaskId", x => x.TaskId, "TaskItems", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_TaskWaitingFors_TaskStakeholders_StakeholderId", x => x.StakeholderId, "TaskStakeholders", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_TaskWaitingFors_WaitingForTypes_WaitingForTypeId", x => x.WaitingForTypeId, "WaitingForTypes", "Id", onDelete: ReferentialAction.Restrict);
                });

            CreateIndexes(migrationBuilder);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TaskAttachments");
            migrationBuilder.DropTable(name: "TaskChecklistItems");
            migrationBuilder.DropTable(name: "TaskComments");
            migrationBuilder.DropTable(name: "TaskLogEntries");
            migrationBuilder.DropTable(name: "TaskRelations");
            migrationBuilder.DropTable(name: "TaskTaskTags");
            migrationBuilder.DropTable(name: "TaskWaitingFors");
            migrationBuilder.DropTable(name: "AttachmentKinds");
            migrationBuilder.DropTable(name: "TaskLogTypes");
            migrationBuilder.DropTable(name: "TaskRelationTypes");
            migrationBuilder.DropTable(name: "TaskTags");
            migrationBuilder.DropTable(name: "TaskStakeholders");
            migrationBuilder.DropTable(name: "WaitingForTypes");
            migrationBuilder.DropTable(name: "StakeholderRoles");
            migrationBuilder.DropTable(name: "StakeholderTypes");
            migrationBuilder.DropTable(name: "TaskItems");
            migrationBuilder.DropTable(name: "BodyFormats");
            migrationBuilder.DropTable(name: "TaskPriorities");
            migrationBuilder.DropTable(name: "TaskSources");
            migrationBuilder.DropTable(name: "TaskStatuses");
            migrationBuilder.DropTable(name: "TaskTypes");
        }

        private static void CreateLookupTable(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.CreateTable(
                name: tableName,
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSystem = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey($"PK_{tableName}", x => x.Id);
                });
        }

        private static void CreateIndexes(MigrationBuilder migrationBuilder)
        {
            foreach (var tableName in new[]
            {
                "AttachmentKinds",
                "BodyFormats",
                "StakeholderRoles",
                "StakeholderTypes",
                "TaskLogTypes",
                "TaskPriorities",
                "TaskRelationTypes",
                "TaskSources",
                "TaskStatuses",
                "TaskTypes",
                "WaitingForTypes"
            })
            {
                migrationBuilder.CreateIndex($"IX_{tableName}_Code", tableName, "Code", unique: true);
            }

            migrationBuilder.CreateIndex("IX_TaskAttachments_AttachmentKindId", "TaskAttachments", "AttachmentKindId");
            migrationBuilder.CreateIndex("IX_TaskAttachments_TaskId", "TaskAttachments", "TaskId");
            migrationBuilder.CreateIndex("IX_TaskChecklistItems_TaskId", "TaskChecklistItems", "TaskId");
            migrationBuilder.CreateIndex("IX_TaskComments_TaskId", "TaskComments", "TaskId");
            migrationBuilder.CreateIndex("IX_TaskItems_BodyFormatId", "TaskItems", "BodyFormatId");
            migrationBuilder.CreateIndex("IX_TaskItems_TaskPriorityId", "TaskItems", "TaskPriorityId");
            migrationBuilder.CreateIndex("IX_TaskItems_TaskSourceId", "TaskItems", "TaskSourceId");
            migrationBuilder.CreateIndex("IX_TaskItems_TaskStatusId", "TaskItems", "TaskStatusId");
            migrationBuilder.CreateIndex("IX_TaskItems_TaskTypeId", "TaskItems", "TaskTypeId");
            migrationBuilder.CreateIndex("IX_TaskLogEntries_TaskId", "TaskLogEntries", "TaskId");
            migrationBuilder.CreateIndex("IX_TaskLogEntries_TaskLogTypeId", "TaskLogEntries", "TaskLogTypeId");
            migrationBuilder.CreateIndex("IX_TaskRelations_SourceTaskId", "TaskRelations", "SourceTaskId");
            migrationBuilder.CreateIndex("IX_TaskRelations_TargetTaskId", "TaskRelations", "TargetTaskId");
            migrationBuilder.CreateIndex("IX_TaskRelations_TaskRelationTypeId", "TaskRelations", "TaskRelationTypeId");
            migrationBuilder.CreateIndex("IX_TaskStakeholders_StakeholderRoleId", "TaskStakeholders", "StakeholderRoleId");
            migrationBuilder.CreateIndex("IX_TaskStakeholders_StakeholderTypeId", "TaskStakeholders", "StakeholderTypeId");
            migrationBuilder.CreateIndex("IX_TaskStakeholders_TaskId", "TaskStakeholders", "TaskId");
            migrationBuilder.CreateIndex("IX_TaskTags_Name", "TaskTags", "Name", unique: true);
            migrationBuilder.CreateIndex("IX_TaskTaskTags_TaskTagId", "TaskTaskTags", "TaskTagId");
            migrationBuilder.CreateIndex("IX_TaskWaitingFors_StakeholderId", "TaskWaitingFors", "StakeholderId");
            migrationBuilder.CreateIndex("IX_TaskWaitingFors_TaskId", "TaskWaitingFors", "TaskId", unique: true, filter: "ResolvedAt IS NULL");
            migrationBuilder.CreateIndex("IX_TaskWaitingFors_WaitingForTypeId", "TaskWaitingFors", "WaitingForTypeId");
        }
    }
}
