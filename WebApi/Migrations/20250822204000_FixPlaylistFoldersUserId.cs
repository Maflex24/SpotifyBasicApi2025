using Microsoft.EntityFrameworkCore.Migrations;

namespace SpotifyBasicApi2025.Migrations
{
    public partial class FixPlaylistFoldersUserId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń potencjalny błędny FK jeśli istnieje (gdyby poprzednia migracja przeszła częściowo)
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_LibPlaylistFolders_SpUsers_UserId",
                    table: "LibPlaylistFolders");
            }
            catch { }

            // Upewnij się, że kolumna ma właściwy typ/długość
            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            // Utwórz indeks jeśli nie istnieje
            try
            {
                migrationBuilder.CreateIndex(
                    name: "IX_LibPlaylistFolders_UserId",
                    table: "LibPlaylistFolders",
                    column: "UserId");
            }
            catch { }

            // Dodaj poprawny FK z NO ACTION (brak kaskad)
            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            try
            {
                migrationBuilder.DropForeignKey(
                    name: "FK_LibPlaylistFolders_SpUsers_UserId",
                    table: "LibPlaylistFolders");
            }
            catch { }

            try
            {
                migrationBuilder.DropIndex(
                    name: "IX_LibPlaylistFolders_UserId",
                    table: "LibPlaylistFolders");
            }
            catch { }
        }
    }
}


