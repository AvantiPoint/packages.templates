using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace NuGetFeedTemplate.Migrations
{
    public partial class PackageGroups : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PackageGroups",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageGroups", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "PublishTargets",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublishEndpoint = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApiToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Legacy = table.Column<bool>(type: "bit", nullable: false),
                    AddedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishTargets", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "PackageGroupMembers",
                columns: table => new
                {
                    PackageGroupName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PackageId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageGroupMembers", x => new { x.PackageGroupName, x.PackageId });
                    table.ForeignKey(
                        name: "FK_PackageGroupMembers_PackageGroups_PackageGroupName",
                        column: x => x.PackageGroupName,
                        principalTable: "PackageGroups",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Syndications",
                columns: table => new
                {
                    PackageGroupName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublishTargetName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Syndications", x => new { x.PackageGroupName, x.PublishTargetName });
                    table.ForeignKey(
                        name: "FK_Syndications_PackageGroups_PackageGroupName",
                        column: x => x.PackageGroupName,
                        principalTable: "PackageGroups",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Syndications_PublishTargets_PublishTargetName",
                        column: x => x.PublishTargetName,
                        principalTable: "PublishTargets",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Syndications_PublishTargetName",
                table: "Syndications",
                column: "PublishTargetName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PackageGroupMembers");

            migrationBuilder.DropTable(
                name: "Syndications");

            migrationBuilder.DropTable(
                name: "PackageGroups");

            migrationBuilder.DropTable(
                name: "PublishTargets");
        }
    }
}
