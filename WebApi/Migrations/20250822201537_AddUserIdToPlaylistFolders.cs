using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddUserIdToPlaylistFolders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "LibPlaylistFolders");
        }
    }
}
