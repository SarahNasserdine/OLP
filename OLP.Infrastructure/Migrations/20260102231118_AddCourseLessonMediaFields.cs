using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCourseLessonMediaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Thumbnail",
                table: "Courses",
                newName: "ThumbnailUrl");

            migrationBuilder.AddColumn<string>(
                name: "AttachmentPublicId",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoPublicId",
                table: "Lessons",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailPublicId",
                table: "Courses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachmentPublicId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "VideoPublicId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "ThumbnailPublicId",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "ThumbnailUrl",
                table: "Courses",
                newName: "Thumbnail");
        }
    }
}
