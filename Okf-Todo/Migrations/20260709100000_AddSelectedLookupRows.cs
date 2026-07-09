using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Photino.Okf_Todo.Data;

#nullable disable

namespace Photino.Okf_Todo.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260709100000_AddSelectedLookupRows")]
    public partial class AddSelectedLookupRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var tableName in LookupTableNames)
            {
                AddSelectedColumn(migrationBuilder, tableName);
            }

            migrationBuilder.Sql("UPDATE TaskTypes SET IsSelected = 0");
            migrationBuilder.Sql("UPDATE TaskTypes SET IsSelected = 1 WHERE Code = 'REQUEST'");
            migrationBuilder.Sql("UPDATE TaskPriorities SET IsSelected = 0");
            migrationBuilder.Sql("UPDATE TaskPriorities SET IsSelected = 1 WHERE Code = 'NORMAL'");

            foreach (var tableName in LookupTableNames)
            {
                CreateSelectionTriggers(migrationBuilder, tableName);
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var tableName in LookupTableNames)
            {
                DropSelectionTriggers(migrationBuilder, tableName);
            }

            foreach (var tableName in LookupTableNames)
            {
                migrationBuilder.DropColumn(name: "IsSelected", table: tableName);
            }
        }

        private static void AddSelectedColumn(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSelected",
                table: tableName,
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        private static void CreateSelectionTriggers(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER IF NOT EXISTS TR_{tableName}_IsSelected_AfterInsert
                AFTER INSERT ON {tableName}
                WHEN NEW.IsSelected = 1
                BEGIN
                    UPDATE {tableName}
                    SET IsSelected = 0
                    WHERE Id <> NEW.Id
                      AND IsSelected = 1;
                END
                """);

            migrationBuilder.Sql($"""
                CREATE TRIGGER IF NOT EXISTS TR_{tableName}_IsSelected_AfterUpdate
                AFTER UPDATE OF IsSelected ON {tableName}
                WHEN NEW.IsSelected = 1
                BEGIN
                    UPDATE {tableName}
                    SET IsSelected = 0
                    WHERE Id <> NEW.Id
                      AND IsSelected = 1;
                END
                """);
        }

        private static void DropSelectionTriggers(MigrationBuilder migrationBuilder, string tableName)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS TR_{tableName}_IsSelected_AfterInsert");
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS TR_{tableName}_IsSelected_AfterUpdate");
        }

        private static readonly string[] LookupTableNames =
        [
            "AttachmentKinds",
            "BodyFormats",
            "StakeholderRoles",
            "StakeholderTypes",
            "TaskLogTypes",
            "TaskPriorities",
            "TaskRelationTypes",
            "TaskSources",
            "TaskStatuses",
            "TaskTypes"
        ];
    }
}
