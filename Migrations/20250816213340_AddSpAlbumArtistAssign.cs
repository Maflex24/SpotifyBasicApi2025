using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddSpAlbumArtistAssign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpAlbums_SpArtists_ArtistId",
                table: "SpAlbums");

            migrationBuilder.RenameColumn(
                name: "ArtistId",
                table: "SpAlbums",
                newName: "SpArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_SpAlbums_ArtistId",
                table: "SpAlbums",
                newName: "IX_SpAlbums_SpArtistId");

            migrationBuilder.CreateTable(
                name: "SpAlbumArtistAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpAlbumArtistAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpAlbumArtistAssigns_SpAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "SpAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpAlbumArtistAssigns_SpArtists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbumArtistAssigns_AlbumId",
                table: "SpAlbumArtistAssigns",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbumArtistAssigns_AlbumId_ArtistId",
                table: "SpAlbumArtistAssigns",
                columns: new[] { "AlbumId", "ArtistId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbumArtistAssigns_ArtistId",
                table: "SpAlbumArtistAssigns",
                column: "ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_SpAlbums_SpArtists_SpArtistId",
                table: "SpAlbums",
                column: "SpArtistId",
                principalTable: "SpArtists",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SpAlbums_SpArtists_SpArtistId",
                table: "SpAlbums");

            migrationBuilder.DropTable(
                name: "SpAlbumArtistAssigns");

            migrationBuilder.RenameColumn(
                name: "SpArtistId",
                table: "SpAlbums",
                newName: "ArtistId");

            migrationBuilder.RenameIndex(
                name: "IX_SpAlbums_SpArtistId",
                table: "SpAlbums",
                newName: "IX_SpAlbums_ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_SpAlbums_SpArtists_ArtistId",
                table: "SpAlbums",
                column: "ArtistId",
                principalTable: "SpArtists",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
