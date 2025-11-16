using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NuGetFeedTemplate.Migrations;

/// <inheritdoc />
public partial class AddJwtAuthenticationSupport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "FirstName",
            table: "Users",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "LastName",
            table: "Users",
            type: "nvarchar(100)",
            maxLength: 100,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProfilePictureUrl",
            table: "Users",
            type: "nvarchar(500)",
            maxLength: 500,
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CreatedAt",
            table: "Users",
            type: "datetimeoffset",
            nullable: false,
            defaultValueSql: "SYSDATETIMEOFFSET()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "LastLoginAt",
            table: "Users",
            type: "datetimeoffset",
            nullable: true);

        // Backfill CreatedAt from earliest AuthToken for each user
        migrationBuilder.Sql(@"
            UPDATE u
            SET u.CreatedAt = ISNULL(
                (SELECT MIN(at.Created) 
                 FROM AuthTokens at 
                 WHERE at.UserEmail = u.Email),
                SYSDATETIMEOFFSET()
            )
            FROM Users u
        ");

        migrationBuilder.CreateTable(
            name: "RefreshTokens",
            columns: table => new
            {
                Token = table.Column<string>(type: "nvarchar(450)", nullable: false),
                UserEmail = table.Column<string>(type: "nvarchar(450)", nullable: true),
                Created = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                Expires = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "DATEADD(day, 7, SYSDATETIMEOFFSET())"),
                IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshTokens", x => x.Token);
                table.ForeignKey(
                    name: "FK_RefreshTokens_Users_UserEmail",
                    column: x => x.UserEmail,
                    principalTable: "Users",
                    principalColumn: "Email");
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshTokens_UserEmail",
            table: "RefreshTokens",
            column: "UserEmail");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RefreshTokens");

        migrationBuilder.DropColumn(
            name: "FirstName",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "LastName",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "ProfilePictureUrl",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "CreatedAt",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "LastLoginAt",
            table: "Users");
    }
}

