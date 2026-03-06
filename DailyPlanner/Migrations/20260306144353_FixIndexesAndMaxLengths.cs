using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyPlanner.Migrations
{
    /// <inheritdoc />
    public partial class FixIndexesAndMaxLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPlans_Date",
                table: "DailyPlans");

            migrationBuilder.DropIndex(
                name: "IX_DailyPlans_WeekId",
                table: "DailyPlans");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlans_WeekId_Date",
                table: "DailyPlans",
                columns: new[] { "WeekId", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DailyPlans_WeekId_Date",
                table: "DailyPlans");

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlans_Date",
                table: "DailyPlans",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyPlans_WeekId",
                table: "DailyPlans",
                column: "WeekId");
        }
    }
}
