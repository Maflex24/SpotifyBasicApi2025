using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class FixUserIdFkDeleteBehavior : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(128)",
                oldMaxLength: 128);

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
