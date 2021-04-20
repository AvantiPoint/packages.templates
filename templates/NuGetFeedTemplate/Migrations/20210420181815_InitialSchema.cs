using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGetFeedTemplate.Migrations
{
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackagePublisher = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "AuthTokens",
                columns: table => new
                {
                    Key = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    Expires = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "DATEADD(year, 1, SYSDATETIMEOFFSET())"),
                    Revoked = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthTokens", x => x.Key);
                    table.ForeignKey(
                        name: "FK_AuthTokens_Users_UserEmail",
                        column: x => x.UserEmail,
                        principalTable: "Users",
                        principalColumn: "Email",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Downloads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Downloaded = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AuthTokenKey = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageVersion = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Downloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Downloads_AuthTokens_AuthTokenKey",
                        column: x => x.AuthTokenKey,
                        principalTable: "AuthTokens",
                        principalColumn: "Key",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthTokens_UserEmail",
                table: "AuthTokens",
                column: "UserEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Downloads_AuthTokenKey",
                table: "Downloads",
                column: "AuthTokenKey");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Downloads");

            migrationBuilder.DropTable(
                name: "AuthTokens");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
