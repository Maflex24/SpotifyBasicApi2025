using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class RemoveHardFKFromLibTagAssigns : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibTagAssigns_SpAlbums_SpotifyItemId",
                table: "LibTagAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibTagAssigns_SpArtists_SpotifyItemId",
                table: "LibTagAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibTagAssigns_SpPlaylists_SpotifyItemId",
                table: "LibTagAssigns");

            migrationBuilder.DropForeignKey(
                name: "FK_LibTagAssigns_SpTracks_SpotifyItemId",
                table: "LibTagAssigns");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_LibTagAssigns_SpAlbums_SpotifyItemId",
                table: "LibTagAssigns",
                column: "SpotifyItemId",
                principalTable: "SpAlbums",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibTagAssigns_SpArtists_SpotifyItemId",
                table: "LibTagAssigns",
                column: "SpotifyItemId",
                principalTable: "SpArtists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibTagAssigns_SpPlaylists_SpotifyItemId",
                table: "LibTagAssigns",
                column: "SpotifyItemId",
                principalTable: "SpPlaylists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LibTagAssigns_SpTracks_SpotifyItemId",
                table: "LibTagAssigns",
                column: "SpotifyItemId",
                principalTable: "SpTracks",
                principalColumn: "Id");
        }
    }
}
