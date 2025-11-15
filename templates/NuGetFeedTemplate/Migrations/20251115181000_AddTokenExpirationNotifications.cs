using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGetFeedTemplate.Migrations
{
    public partial class AddTokenExpirationNotifications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TokenExpirationNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenKey = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    NotificationType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenExpirationNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TokenExpirationNotifications_AuthTokens_TokenKey",
                        column: x => x.TokenKey,
                        principalTable: "AuthTokens",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenExpirationNotifications_TokenKey",
                table: "TokenExpirationNotifications",
                column: "TokenKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenExpirationNotifications");
        }
    }
}
