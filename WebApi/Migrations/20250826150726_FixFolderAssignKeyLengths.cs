using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    /// <inheritdoc />
    public partial class FixFolderAssignKeyLengths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FKs zależne od modyfikowanych kolumn
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpUsers_UserId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpPlaylists_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders");

            // Drop indeksy zależne od modyfikowanych kolumn
            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_UserId_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            // Dodatkowo: zależności od FolderId w Assigns
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_LibPlaylistFolders_FolderId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_FolderId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(450)",
                oldMaxLength: 450)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolderAssigns",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(450)",
                oldMaxLength: 450)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistId",
                table: "LibPlaylistFolderAssigns",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(450)",
                oldMaxLength: 450)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // Odtworzenie indeksów
            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_UserId_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                columns: new[] { "UserId", "PlaylistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_FolderId",
                table: "LibPlaylistFolderAssigns",
                column: "FolderId");

            // Odtworzenie FKs
            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpUsers_UserId",
                table: "LibPlaylistFolderAssigns",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpPlaylists_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                column: "PlaylistId",
                principalTable: "SpPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_LibPlaylistFolders_FolderId",
                table: "LibPlaylistFolderAssigns",
                column: "FolderId",
                principalTable: "LibPlaylistFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FKs i indeksy, aby cofnąć zmiany kolumn
            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpUsers_UserId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpPlaylists_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_UserId_PlaylistId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibPlaylistFolderAssigns_LibPlaylistFolders_FolderId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolderAssigns_FolderId",
                table: "LibPlaylistFolderAssigns");

            migrationBuilder.DropIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolders",
                type: "varchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "LibPlaylistFolderAssigns",
                type: "varchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "PlaylistId",
                table: "LibPlaylistFolderAssigns",
                type: "varchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            // Odtworzenie pierwotnych indeksów
            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_UserId_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                columns: new[] { "UserId", "PlaylistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_FolderId",
                table: "LibPlaylistFolderAssigns",
                column: "FolderId");

            // Odtworzenie pierwotnych FKs
            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpUsers_UserId",
                table: "LibPlaylistFolderAssigns",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_SpPlaylists_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                column: "PlaylistId",
                principalTable: "SpPlaylists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolders_SpUsers_UserId",
                table: "LibPlaylistFolders",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibPlaylistFolderAssigns_LibPlaylistFolders_FolderId",
                table: "LibPlaylistFolderAssigns",
                column: "FolderId",
                principalTable: "LibPlaylistFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
