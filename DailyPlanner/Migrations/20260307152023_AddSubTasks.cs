using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyPlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddSubTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentTaskId",
                table: "DailyTasks",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyTasks_ParentTaskId",
                table: "DailyTasks",
                column: "ParentTaskId");

            migrationBuilder.AddForeignKey(
                name: "FK_DailyTasks_DailyTasks_ParentTaskId",
                table: "DailyTasks",
                column: "ParentTaskId",
                principalTable: "DailyTasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DailyTasks_DailyTasks_ParentTaskId",
                table: "DailyTasks");

            migrationBuilder.DropIndex(
                name: "IX_DailyTasks_ParentTaskId",
                table: "DailyTasks");

            migrationBuilder.DropColumn(
                name: "ParentTaskId",
                table: "DailyTasks");
        }
    }
}
