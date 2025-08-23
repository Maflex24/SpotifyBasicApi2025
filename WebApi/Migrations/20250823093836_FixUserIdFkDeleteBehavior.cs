using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class FixUserIdFkDeleteBehavior : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.FK_LibPlaylistFolders_SpUsers_UserId','F') IS NOT NULL
ALTER TABLE [LibPlaylistFolders] DROP CONSTRAINT [FK_LibPlaylistFolders_SpUsers_UserId];");

            // Długość kolumny została już ustawiona w poprzedniej migracji na nvarchar(450)

            migrationBuilder.Sql(@"IF OBJECT_ID('dbo.FK_LibPlaylistFolders_SpUsers_UserId','F') IS NULL
ALTER TABLE [LibPlaylistFolders] WITH NOCHECK ADD CONSTRAINT [FK_LibPlaylistFolders_SpUsers_UserId]
FOREIGN KEY([UserId]) REFERENCES [SpUsers]([Id]);");
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
