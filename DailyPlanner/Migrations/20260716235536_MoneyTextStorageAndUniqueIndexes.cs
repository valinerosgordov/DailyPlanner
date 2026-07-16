using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyPlanner.Migrations
{
    /// <inheritdoc />
    public partial class MoneyTextStorageAndUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxTasks_ExternalId",
                table: "InboxTasks");

            migrationBuilder.DropIndex(
                name: "IX_HabitEntries_HabitDefinitionId",
                table: "HabitEntries");

            migrationBuilder.DropIndex(
                name: "IX_FinanceCategories_SeedKey",
                table: "FinanceCategories");

            migrationBuilder.AlterColumn<decimal>(
                name: "ListPriceMonthly",
                table: "RecurringPayments",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "RecurringPayments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalMonthlyAmount",
                table: "IncomeSources",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "IncomeSourcePayments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetAmount",
                table: "FinancialGoals",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SavedAmount",
                table: "FinancialGoals",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "FinanceEntries",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountInBaseCurrency",
                table: "FinanceEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceEntries",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceBudgets",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "ExchangeRates",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Debts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DebtPayments",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AccountTransfers",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialBalance",
                table: "Accounts",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateIndex(
                name: "IX_InboxTasks_ExternalId",
                table: "InboxTasks",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_HabitEntries_HabitDefinitionId_DayOfWeek",
                table: "HabitEntries",
                columns: new[] { "HabitDefinitionId", "DayOfWeek" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_SeedKey",
                table: "FinanceCategories",
                column: "SeedKey",
                unique: true,
                filter: "\"SeedKey\" IS NOT NULL");

            // Backfill: rows saved before base-currency stamping have
            // AmountInBaseCurrency = 0. All such rows predate multi-currency
            // support, so their raw Amount IS the base-currency value. Aggregates
            // fall back to Amount when the base value is 0 (see FinanceEntry
            // .BaseAmount), but stamping the stored value keeps the schema honest.
            // CAST tolerates both '0' (from INTEGER storage) and '0.0' text forms.
            migrationBuilder.Sql(
                """
                UPDATE "FinanceEntries"
                SET "AmountInBaseCurrency" = "Amount"
                WHERE CAST("AmountInBaseCurrency" AS REAL) = 0.0;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InboxTasks_ExternalId",
                table: "InboxTasks");

            migrationBuilder.DropIndex(
                name: "IX_HabitEntries_HabitDefinitionId_DayOfWeek",
                table: "HabitEntries");

            migrationBuilder.DropIndex(
                name: "IX_FinanceCategories_SeedKey",
                table: "FinanceCategories");

            migrationBuilder.AlterColumn<decimal>(
                name: "ListPriceMonthly",
                table: "RecurringPayments",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "RecurringPayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalMonthlyAmount",
                table: "IncomeSources",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "IncomeSourcePayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "TargetAmount",
                table: "FinancialGoals",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "SavedAmount",
                table: "FinancialGoals",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "FinanceEntries",
                type: "decimal(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "AmountInBaseCurrency",
                table: "FinanceEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldDefaultValue: 0m);

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceEntries",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "FinanceBudgets",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Rate",
                table: "ExchangeRates",
                type: "decimal(18,6)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "Debts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "DebtPayments",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "AccountTransfers",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<decimal>(
                name: "InitialBalance",
                table: "Accounts",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_InboxTasks_ExternalId",
                table: "InboxTasks",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_HabitEntries_HabitDefinitionId",
                table: "HabitEntries",
                column: "HabitDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceCategories_SeedKey",
                table: "FinanceCategories",
                column: "SeedKey");
        }
    }
}
