using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InvestorChat.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPortfolios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPortfolios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPortfolios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPortfolios_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortfolioPositions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PortfolioId = table.Column<int>(type: "integer", nullable: false),
                    SecurityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Exchange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    BlockedQuantity = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    Price = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    PriceCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(20,6)", precision: 20, scale: 6, nullable: false),
                    ValueCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortfolioPositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortfolioPositions_UserPortfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "UserPortfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PortfolioPositions_PortfolioId",
                table: "PortfolioPositions",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPortfolios_UserId_Name",
                table: "UserPortfolios",
                columns: new[] { "UserId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PortfolioPositions");

            migrationBuilder.DropTable(
                name: "UserPortfolios");
        }
    }
}
