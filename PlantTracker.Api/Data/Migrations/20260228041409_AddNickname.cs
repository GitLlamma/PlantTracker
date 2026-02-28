using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PlantTracker.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNickname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Nickname",
                table: "UserPlants",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nickname",
                table: "UserPlants");
        }
    }
}
