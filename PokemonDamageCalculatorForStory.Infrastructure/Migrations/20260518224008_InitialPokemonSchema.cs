using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonDamageCalculatorForStory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialPokemonSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Battles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EnemyPokemon = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Battles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalculationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttackerParams = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DefenderParams = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DamageRollsJson = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculationResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OwnPokemonSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BattleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Species = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Stats = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EVs = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OwnPokemonSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RuleSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Generation = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RuleSets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    RuleSetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAuthorizationInfos",
                columns: table => new
                {
                    GoogleUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuthorizationInfos", x => x.GoogleUserId);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoogleUserId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Permission = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserPermissions_UserAuthorizationInfos_GoogleUserId",
                        column: x => x.GoogleUserId,
                        principalTable: "UserAuthorizationInfos",
                        principalColumn: "GoogleUserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RuleSets_Slug",
                table: "RuleSets",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserPermissions_GoogleUserId_Permission",
                table: "UserPermissions",
                columns: new[] { "GoogleUserId", "Permission" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Battles");

            migrationBuilder.DropTable(
                name: "CalculationResults");

            migrationBuilder.DropTable(
                name: "OwnPokemonSnapshots");

            migrationBuilder.DropTable(
                name: "RuleSets");

            migrationBuilder.DropTable(
                name: "Runs");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserAuthorizationInfos");
        }
    }
}
