using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OkfTodo.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskSampleDataMarker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSampleData",
                table: "TaskItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_TaskItems_IsSampleData",
                table: "TaskItems",
                column: "IsSampleData");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskItems_IsSampleData",
                table: "TaskItems");

            migrationBuilder.DropColumn(
                name: "IsSampleData",
                table: "TaskItems");
        }
    }
}
