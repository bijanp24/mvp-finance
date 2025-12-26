using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FinanceEngine.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Icon = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Color = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "Icon", "IsActive", "Name", "SortOrder", "Type" },
                values: new object[,]
                {
                    { 1, "#5C6BC0", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "home", true, "Housing", 1, 0 },
                    { 2, "#FFA726", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "bolt", true, "Utilities", 2, 0 },
                    { 3, "#26A69A", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shield", true, "Insurance", 3, 0 },
                    { 4, "#AB47BC", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "subscriptions", true, "Subscriptions", 4, 0 },
                    { 5, "#42A5F5", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "wifi", true, "Phone & Internet", 5, 0 },
                    { 6, "#66BB6A", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shopping_cart", true, "Groceries", 10, 1 },
                    { 7, "#EF5350", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "restaurant", true, "Dining", 11, 1 },
                    { 8, "#78909C", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "directions_car", true, "Transportation", 12, 1 },
                    { 9, "#EC407A", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "movie", true, "Entertainment", 13, 1 },
                    { 10, "#7E57C2", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "shopping_bag", true, "Shopping", 14, 1 },
                    { 11, "#26C6DA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "fitness_center", true, "Health & Fitness", 15, 1 },
                    { 12, "#FFCA28", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "spa", true, "Personal Care", 16, 1 },
                    { 13, "#8D6E63", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "school", true, "Education", 17, 1 },
                    { 14, "#F48FB1", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "card_giftcard", true, "Gifts & Donations", 18, 1 },
                    { 15, "#BDBDBD", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "more_horiz", true, "Other", 99, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive",
                table: "Categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Type",
                table: "Categories",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
