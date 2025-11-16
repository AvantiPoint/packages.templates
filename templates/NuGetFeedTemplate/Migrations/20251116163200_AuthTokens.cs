using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NuGetFeedTemplate.Migrations
{
    /// <inheritdoc />
    public partial class AuthTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenNotifications_AuthTokens_TokenKey",
                table: "TokenNotifications");

            migrationBuilder.AddColumn<int>(
                name: "NotificationTypeEnum",
                table: "TokenNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddForeignKey(
                name: "FK_TokenNotifications_AuthTokens_TokenKey",
                table: "TokenNotifications",
                column: "TokenKey",
                principalTable: "AuthTokens",
                principalColumn: "Key");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TokenNotifications_AuthTokens_TokenKey",
                table: "TokenNotifications");

            migrationBuilder.DropColumn(
                name: "NotificationTypeEnum",
                table: "TokenNotifications");

            migrationBuilder.AddForeignKey(
                name: "FK_TokenNotifications_AuthTokens_TokenKey",
                table: "TokenNotifications",
                column: "TokenKey",
                principalTable: "AuthTokens",
                principalColumn: "Key",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
