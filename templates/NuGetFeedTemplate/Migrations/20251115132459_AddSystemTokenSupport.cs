using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGetFeedTemplate.Migrations
{
    public partial class AddSystemTokenSupport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add IsSystemToken column to AuthTokens table
            migrationBuilder.AddColumn<bool>(
                name: "IsSystemToken",
                table: "AuthTokens",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove system tokens
            migrationBuilder.Sql("DELETE FROM AuthTokens WHERE IsSystemToken = 1");

            // Remove the IsSystemToken column
            migrationBuilder.DropColumn(
                name: "IsSystemToken",
                table: "AuthTokens");
        }
    }
}
