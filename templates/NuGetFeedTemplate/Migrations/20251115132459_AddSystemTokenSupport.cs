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

            // Create a system token for each existing user that expires in 24 hours
            migrationBuilder.Sql(@"
                INSERT INTO AuthTokens (Key, Description, UserEmail, Created, Expires, Revoked, IsSystemToken)
                SELECT 
                    LOWER(CONVERT(VARCHAR(32), HASHBYTES('SHA1', CONCAT(Email, NEWID())), 2)),
                    'System Token',
                    Email,
                    SYSDATETIMEOFFSET(),
                    DATEADD(hour, 24, SYSDATETIMEOFFSET()),
                    0,
                    1
                FROM Users
                WHERE Email NOT IN (SELECT DISTINCT UserEmail FROM AuthTokens WHERE IsSystemToken = 1 AND UserEmail IS NOT NULL)
            ");
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
