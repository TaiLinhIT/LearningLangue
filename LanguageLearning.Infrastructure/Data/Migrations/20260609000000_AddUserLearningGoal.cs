using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LanguageLearning.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(LanguageLearningDbContext))]
    [Migration("20260609000000_AddUserLearningGoal")]
    public partial class AddUserLearningGoal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LearningGoal",
                table: "Users",
                type: "nvarchar(180)",
                maxLength: 180,
                nullable: false,
                defaultValue: "Giao tiep hang ngay");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LearningGoal",
                table: "Users");
        }
    }
}
