using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonDamageCalculatorForStory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotBaseStatsAndIvs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseStats",
                table: "OwnPokemonSnapshots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IVs",
                table: "OwnPokemonSnapshots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseStats",
                table: "OwnPokemonSnapshots");

            migrationBuilder.DropColumn(
                name: "IVs",
                table: "OwnPokemonSnapshots");
        }
    }
}
