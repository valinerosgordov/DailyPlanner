using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DailyPlanner.Migrations
{
    /// <inheritdoc />
    public partial class SubscriptionsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoRenew",
                table: "RecurringPayments",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "BillingIntervalMonths",
                table: "RecurringPayments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "CancellationNoticeDays",
                table: "RecurringPayments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "RecurringPayments",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "RUB");

            migrationBuilder.AddColumn<bool>(
                name: "IsSubscription",
                table: "RecurringPayments",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "LastReviewedDate",
                table: "RecurringPayments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListPriceMonthly",
                table: "RecurringPayments",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "NextRenewalDate",
                table: "RecurringPayments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenewalRemindDaysBefore",
                table: "RecurringPayments",
                type: "TEXT",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "TrialEndsOn",
                table: "RecurringPayments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AmountInBaseCurrency",
                table: "FinanceEntries",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "FinanceEntries",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "RUB");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRateToBase",
                table: "FinanceEntries",
                type: "decimal(18,6)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CurrencyCode = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    BaseCurrency = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_IsSubscription",
                table: "RecurringPayments",
                column: "IsSubscription");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringPayments_NextRenewalDate",
                table: "RecurringPayments",
                column: "NextRenewalDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceEntries_IsPaid",
                table: "FinanceEntries",
                column: "IsPaid");

            migrationBuilder.CreateIndex(
                name: "IX_ExchangeRates_CurrencyCode_BaseCurrency_Date",
                table: "ExchangeRates",
                columns: new[] { "CurrencyCode", "BaseCurrency", "Date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropIndex(
                name: "IX_RecurringPayments_IsSubscription",
                table: "RecurringPayments");

            migrationBuilder.DropIndex(
                name: "IX_RecurringPayments_NextRenewalDate",
                table: "RecurringPayments");

            migrationBuilder.DropIndex(
                name: "IX_FinanceEntries_IsPaid",
                table: "FinanceEntries");

            migrationBuilder.DropColumn(
                name: "AutoRenew",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "BillingIntervalMonths",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "CancellationNoticeDays",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "IsSubscription",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "LastReviewedDate",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "ListPriceMonthly",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "NextRenewalDate",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "RenewalRemindDaysBefore",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "TrialEndsOn",
                table: "RecurringPayments");

            migrationBuilder.DropColumn(
                name: "AmountInBaseCurrency",
                table: "FinanceEntries");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "FinanceEntries");

            migrationBuilder.DropColumn(
                name: "ExchangeRateToBase",
                table: "FinanceEntries");
        }
    }
}
