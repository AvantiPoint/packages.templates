using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGetFeedTemplate.Migrations
{
    public partial class AddUserRevocation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add IsRevoked column to Users table
            migrationBuilder.AddColumn<bool>(
                name: "IsRevoked",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the IsRevoked column
            migrationBuilder.DropColumn(
                name: "IsRevoked",
                table: "Users");
        }
    }
}
