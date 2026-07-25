using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkfTodo.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaskLists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLists", x => x.Id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO "TaskLists" ("Name", "SortOrder", "CreatedAt", "UpdatedAt")
                VALUES ('Default list', 10, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP);
                """);

            migrationBuilder.AddColumn<int>(
                name: "TaskListId",
                table: "TaskItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "TaskItems"
                SET "TaskListId" = (
                    SELECT "Id"
                    FROM "TaskLists"
                    WHERE "Name" = 'Default list'
                    LIMIT 1
                );
                """);

            migrationBuilder.AlterColumn<int>(
                name: "TaskListId",
                table: "TaskItems",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_TaskListId",
                table: "TaskItems",
                column: "TaskListId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLists_Name",
                table: "TaskLists",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskLists_SortOrder",
                table: "TaskLists",
                column: "SortOrder");

            migrationBuilder.AddForeignKey(
                name: "FK_TaskItems_TaskLists_TaskListId",
                table: "TaskItems",
                column: "TaskListId",
                principalTable: "TaskLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaskItems_TaskLists_TaskListId",
                table: "TaskItems");

            migrationBuilder.DropTable(
                name: "TaskLists");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_TaskListId",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "TaskListId",
                table: "TaskItems");
        }
    }
}
