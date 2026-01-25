using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OLP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFinalQuizSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFinal",
                table: "Quizzes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SelectedQuestionIdsJson",
                table: "QuizAttempts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFinal",
                table: "Quizzes");

            migrationBuilder.DropColumn(
                name: "SelectedQuestionIdsJson",
                table: "QuizAttempts");
        }
    }
}
