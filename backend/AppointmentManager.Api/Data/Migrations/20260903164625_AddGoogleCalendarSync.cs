using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AppointmentManager.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleEventId",
                table: "Appointments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "GoogleAuthTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RefreshToken = table.Column<string>(type: "TEXT", nullable: false),
                    AccessToken = table.Column<string>(type: "TEXT", nullable: true),
                    AccessTokenExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleAuthTokens", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleAuthTokens");

            migrationBuilder.DropColumn(
                name: "GoogleEventId",
                table: "Appointments");
        }
    }
}
