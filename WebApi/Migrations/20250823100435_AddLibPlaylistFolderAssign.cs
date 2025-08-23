using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddLibPlaylistFolderAssign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibPlaylistFolderAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    PlaylistId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    FolderId = table.Column<long>(type: "bigint", nullable: true),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibPlaylistFolderAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibPlaylistFolderAssigns_LibPlaylistFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "LibPlaylistFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LibPlaylistFolderAssigns_SpPlaylists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "SpPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibPlaylistFolderAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_FolderId",
                table: "LibPlaylistFolderAssigns",
                column: "FolderId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolderAssigns_UserId_PlaylistId",
                table: "LibPlaylistFolderAssigns",
                columns: new[] { "UserId", "PlaylistId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibPlaylistFolderAssigns");
        }
    }
}
