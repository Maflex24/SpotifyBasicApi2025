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
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders",
                column: "UserId");

            // FK dodamy w kolejnej migracji naprawczej, aby uniknąć cyklicznych kaskad podczas pierwszego wdrożenia
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
