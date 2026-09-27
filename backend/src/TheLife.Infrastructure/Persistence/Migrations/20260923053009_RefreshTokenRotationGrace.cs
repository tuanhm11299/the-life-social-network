using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheLife.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokenRotationGrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "replaced_at",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "replaced_at",
                table: "refresh_tokens");
        }
    }
}
