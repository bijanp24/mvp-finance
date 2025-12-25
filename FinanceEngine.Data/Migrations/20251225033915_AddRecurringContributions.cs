using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringContributions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecurringContributions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Frequency = table.Column<int>(type: "INTEGER", nullable: false),
                    NextContributionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetAccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecurringContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecurringContributions_Accounts_SourceAccountId",
                        column: x => x.SourceAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecurringContributions_Accounts_TargetAccountId",
                        column: x => x.TargetAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecurringContributions_SourceAccountId",
                table: "RecurringContributions",
                column: "SourceAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_RecurringContributions_TargetAccountId",
                table: "RecurringContributions",
                column: "TargetAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecurringContributions");
        }
    }
}
