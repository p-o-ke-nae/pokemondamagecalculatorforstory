using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PokemonDamageCalculatorForStory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddForeignKeyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Runs_RuleSetId",
                table: "Runs",
                column: "RuleSetId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnPokemonSnapshots_BattleId",
                table: "OwnPokemonSnapshots",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_OwnPokemonSnapshots_RunId",
                table: "OwnPokemonSnapshots",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_BattleId",
                table: "CalculationResults",
                column: "BattleId");

            migrationBuilder.CreateIndex(
                name: "IX_CalculationResults_RunId",
                table: "CalculationResults",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_Battles_RunId",
                table: "Battles",
                column: "RunId");

            migrationBuilder.AddForeignKey(
                name: "FK_Battles_Runs_RunId",
                table: "Battles",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Battles_BattleId",
                table: "CalculationResults",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Runs_RunId",
                table: "CalculationResults",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnPokemonSnapshots_Battles_BattleId",
                table: "OwnPokemonSnapshots",
                column: "BattleId",
                principalTable: "Battles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OwnPokemonSnapshots_Runs_RunId",
                table: "OwnPokemonSnapshots",
                column: "RunId",
                principalTable: "Runs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Runs_RuleSets_RuleSetId",
                table: "Runs",
                column: "RuleSetId",
                principalTable: "RuleSets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Battles_Runs_RunId",
                table: "Battles");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Battles_BattleId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Runs_RunId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnPokemonSnapshots_Battles_BattleId",
                table: "OwnPokemonSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_OwnPokemonSnapshots_Runs_RunId",
                table: "OwnPokemonSnapshots");

            migrationBuilder.DropForeignKey(
                name: "FK_Runs_RuleSets_RuleSetId",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Runs_RuleSetId",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_OwnPokemonSnapshots_BattleId",
                table: "OwnPokemonSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_OwnPokemonSnapshots_RunId",
                table: "OwnPokemonSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_CalculationResults_BattleId",
                table: "CalculationResults");

            migrationBuilder.DropIndex(
                name: "IX_CalculationResults_RunId",
                table: "CalculationResults");

            migrationBuilder.DropIndex(
                name: "IX_Battles_RunId",
                table: "Battles");
        }
    }
}
