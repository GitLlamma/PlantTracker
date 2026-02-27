using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomPlantFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareLevel",
                table: "UserPlants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cycle",
                table: "UserPlants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sunlight",
                table: "UserPlants",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Watering",
                table: "UserPlants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareLevel",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "Cycle",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "Sunlight",
                table: "UserPlants");

            migrationBuilder.DropColumn(
                name: "Watering",
                table: "UserPlants");
        }
    }
}
