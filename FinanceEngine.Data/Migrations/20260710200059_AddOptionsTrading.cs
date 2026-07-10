using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinanceEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOptionsTrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OptionContracts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TickerSymbol = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StrikePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OptionType = table.Column<string>(type: "TEXT", nullable: false),
                    Position = table.Column<string>(type: "TEXT", nullable: false),
                    Premium = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptionTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OptionContractId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OptionTransactions_OptionContracts_OptionContractId",
                        column: x => x.OptionContractId,
                        principalTable: "OptionContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptionTransactions_OptionContractId",
                table: "OptionTransactions",
                column: "OptionContractId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OptionTransactions");

            migrationBuilder.DropTable(
                name: "OptionContracts");
        }
    }
}
