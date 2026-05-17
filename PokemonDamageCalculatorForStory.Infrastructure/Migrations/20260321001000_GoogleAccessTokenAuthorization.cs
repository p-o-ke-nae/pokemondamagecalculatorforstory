using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonDamageCalculatorForStory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GoogleAccessTokenAuthorization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublic",
                table: "WeatherForecasts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OwnerGoogleUserId",
                table: "WeatherForecasts",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

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
                name: "IX_UserPermissions_GoogleUserId_Permission",
                table: "UserPermissions",
                columns: new[] { "GoogleUserId", "Permission" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "UserAuthorizationInfos");

            migrationBuilder.DropColumn(
                name: "IsPublic",
                table: "WeatherForecasts");

            migrationBuilder.DropColumn(
                name: "OwnerGoogleUserId",
                table: "WeatherForecasts");
        }
    }
}