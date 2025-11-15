using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NuGetFeedTemplate.Migrations
{
    /// <inheritdoc />
    public partial class DropDuplicateDownloads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Downloads");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Downloads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthTokenKey = table.Column<string>(type: "nvarchar(32)", nullable: true),
                    Downloaded = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageVersion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Downloads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Downloads_AuthTokens_AuthTokenKey",
                        column: x => x.AuthTokenKey,
                        principalTable: "AuthTokens",
                        principalColumn: "Key");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Downloads_AuthTokenKey",
                table: "Downloads",
                column: "AuthTokenKey");
        }
    }
}
