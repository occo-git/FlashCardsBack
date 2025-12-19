using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ResetPasswordRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBookmarks_Users_UserId",
                table: "UserBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens");

            migrationBuilder.AlterColumn<string>(
                name: "PartOfSpeech",
                table: "Words",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "UserWordsProgress",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "ResetPasswordTokens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserBookmarks_Users_UserId",
                table: "UserBookmarks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserBookmarks_Users_UserId",
                table: "UserBookmarks");

            migrationBuilder.DropIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens");

            migrationBuilder.AlterColumn<string>(
                name: "PartOfSpeech",
                table: "Words",
                type: "varchar(50)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "ActivityType",
                table: "UserWordsProgress",
                type: "varchar(20)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "ResetPasswordTokens",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32);

            migrationBuilder.CreateIndex(
                name: "IX_ResetPasswordTokens_UserId",
                table: "ResetPasswordTokens",
                column: "UserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserBookmarks_Users_UserId",
                table: "UserBookmarks",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
