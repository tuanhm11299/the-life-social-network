using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheLife.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPostImageThumbnailUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "thumbnail_url",
                table: "post_images",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            // Posts created before thumbnails existed have no small copy: let them use the full image.
            migrationBuilder.Sql("UPDATE post_images SET thumbnail_url = url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "thumbnail_url",
                table: "post_images");
        }
    }
}
