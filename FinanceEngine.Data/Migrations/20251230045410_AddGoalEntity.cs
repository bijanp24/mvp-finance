using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoalEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    TargetDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LinkedAccountIds = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Goals_IsActive",
                table: "Goals",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_Priority",
                table: "Goals",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_TargetDate",
                table: "Goals",
                column: "TargetDate");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_Type",
                table: "Goals",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Goals");
        }
    }
}
