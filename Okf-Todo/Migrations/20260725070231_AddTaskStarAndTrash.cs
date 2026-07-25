using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkfTodo.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskStarAndTrash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TaskItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsStarred",
                table: "TaskItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "StarredAt",
                table: "TaskItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_DeletedAt_IsStarred",
                table: "TaskItems",
                columns: new[] { "DeletedAt", "IsStarred" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_StarredAt",
                table: "TaskItems",
                column: "StarredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_DeletedAt_IsStarred",
                table: "TaskItems");

            migrationBuilder.DropIndex(
                name: "IX_TaskItems_StarredAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IsStarred",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "StarredAt",
                table: "TaskItems");
        }
    }
}
