using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonDamageCalculatorForStory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSnapshotBaseStatsAndIvs : Migration
    {
        private const string DefaultBaseStatsJson = "{\"Hp\":1,\"Attack\":1,\"Defense\":1,\"SpecialAttack\":1,\"SpecialDefense\":1,\"Speed\":1}";
        private const string DefaultIvsJson = "{\"Hp\":0,\"Attack\":0,\"Defense\":0,\"SpecialAttack\":0,\"SpecialDefense\":0,\"Speed\":0}";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseStats",
                table: "OwnPokemonSnapshots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: DefaultBaseStatsJson);

            migrationBuilder.AddColumn<string>(
                name: "IVs",
                table: "OwnPokemonSnapshots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: DefaultIvsJson);
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
