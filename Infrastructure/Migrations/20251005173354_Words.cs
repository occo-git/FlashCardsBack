using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Words : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastActive",
                table: "Users",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Level",
                table: "Users",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Words",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Word = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PartOfSpeech = table.Column<string>(type: "varchar(50)", maxLength: 20, nullable: false),
                    Transcription = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Translation = table.Column<string>(type: "text", nullable: false),
                    Example = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AudioUrl = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Level = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Mark = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Words", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FillBlanks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WordId = table.Column<long>(type: "bigint", nullable: false),
                    BlankTemplate = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FillBlanks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FillBlanks_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WordThemes",
                columns: table => new
                {
                    WordId = table.Column<long>(type: "bigint", nullable: false),
                    ThemeId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WordThemes", x => new { x.WordId, x.ThemeId });
                    table.ForeignKey(
                        name: "FK_WordThemes_Themes_ThemeId",
                        column: x => x.ThemeId,
                        principalTable: "Themes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WordThemes_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserWordsProgress",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    WordId = table.Column<long>(type: "bigint", nullable: false),
                    ActivityType = table.Column<string>(type: "varchar(20)", nullable: false),
                    FillBlankId = table.Column<long>(type: "bigint", nullable: true),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    TotalAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextReview = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SuccessRate = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWordsProgress", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserWordsProgress_FillBlanks_FillBlankId",
                        column: x => x.FillBlankId,
                        principalTable: "FillBlanks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserWordsProgress_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWordsProgress_Words_WordId",
                        column: x => x.WordId,
                        principalTable: "Words",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FillBlanks_WordId",
                table: "FillBlanks",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWordsProgress_FillBlankId",
                table: "UserWordsProgress",
                column: "FillBlankId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWordsProgress_UserId",
                table: "UserWordsProgress",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWordsProgress_WordId",
                table: "UserWordsProgress",
                column: "WordId");

            migrationBuilder.CreateIndex(
                name: "IX_WordThemes_ThemeId",
                table: "WordThemes",
                column: "ThemeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserWordsProgress");

            migrationBuilder.DropTable(
                name: "WordThemes");

            migrationBuilder.DropTable(
                name: "FillBlanks");

            migrationBuilder.DropTable(
                name: "Themes");

            migrationBuilder.DropTable(
                name: "Words");

            migrationBuilder.DropColumn(
                name: "LastActive",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Level",
                table: "Users");
        }
    }
}
