using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddPlaylistFolders : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FolderId",
                table: "SpPlaylists",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LibPlaylistFolders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibPlaylistFolders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibPlaylistFolders_LibPlaylistFolders_ParentId",
                        column: x => x.ParentId,
                        principalTable: "LibPlaylistFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylists_FolderId",
                table: "SpPlaylists",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_ParentId",
                table: "LibPlaylistFolders",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_SpPlaylists_LibPlaylistFolders_FolderId",
                table: "SpPlaylists",
                column: "FolderId",
                principalTable: "LibPlaylistFolders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpPlaylists_LibPlaylistFolders_FolderId",
                table: "SpPlaylists");

            migrationBuilder.DropTable(
                name: "LibPlaylistFolders");

            migrationBuilder.DropIndex(
                name: "IX_SpPlaylists_FolderId",
                table: "SpPlaylists");

            migrationBuilder.DropColumn(
                name: "FolderId",
                table: "SpPlaylists");
        }
    }
}
