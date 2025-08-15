using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddSpotifyEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpArtists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Genres = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpArtists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Country = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Product = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Followers = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpAlbums",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AlbumType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalTracks = table.Column<int>(type: "int", nullable: false),
                    AvailableMarkets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleaseDate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReleaseDatePrecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpAlbums_SpArtists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SpPlaylists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Collaborative = table.Column<bool>(type: "bit", nullable: false),
                    Public = table.Column<bool>(type: "bit", nullable: false),
                    TotalTracks = table.Column<int>(type: "int", nullable: false),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpPlaylists_SpUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SpSavedAlbums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpSavedAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpSavedAlbums_SpAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "SpAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpSavedAlbums_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpTracks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Href = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Explicit = table.Column<bool>(type: "bit", nullable: false),
                    DiscNumber = table.Column<int>(type: "int", nullable: false),
                    TrackNumber = table.Column<int>(type: "int", nullable: false),
                    AvailableMarkets = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    IsPlayable = table.Column<bool>(type: "bit", nullable: false),
                    IsLocal = table.Column<bool>(type: "bit", nullable: false),
                    PreviewUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Images = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpTracks_SpAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "SpAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SpPlaylistTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlaylistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrackId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpPlaylistTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpPlaylistTracks_SpPlaylists_PlaylistId",
                        column: x => x.PlaylistId,
                        principalTable: "SpPlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpPlaylistTracks_SpTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "SpTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpSavedTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrackId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpSavedTracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpSavedTracks_SpTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "SpTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpSavedTracks_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpTrackArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrackId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpTrackArtists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpTrackArtists_SpArtists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpTrackArtists_SpTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "SpTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbums_ArtistId",
                table: "SpAlbums",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbums_Name",
                table: "SpAlbums",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpArtists_Name",
                table: "SpArtists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylists_Name",
                table: "SpPlaylists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylists_OwnerId",
                table: "SpPlaylists",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylistTracks_PlaylistId",
                table: "SpPlaylistTracks",
                column: "PlaylistId");

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylistTracks_TrackId",
                table: "SpPlaylistTracks",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_SpSavedAlbums_AlbumId",
                table: "SpSavedAlbums",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_SpSavedAlbums_UserId",
                table: "SpSavedAlbums",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpSavedTracks_TrackId",
                table: "SpSavedTracks",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_SpSavedTracks_UserId",
                table: "SpSavedTracks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SpTrackArtists_ArtistId",
                table: "SpTrackArtists",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_SpTrackArtists_TrackId",
                table: "SpTrackArtists",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_SpTracks_AlbumId",
                table: "SpTracks",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_SpTracks_Name",
                table: "SpTracks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpUsers_DisplayName",
                table: "SpUsers",
                column: "DisplayName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpPlaylistTracks");

            migrationBuilder.DropTable(
                name: "SpSavedAlbums");

            migrationBuilder.DropTable(
                name: "SpSavedTracks");

            migrationBuilder.DropTable(
                name: "SpTrackArtists");

            migrationBuilder.DropTable(
                name: "SpPlaylists");

            migrationBuilder.DropTable(
                name: "SpTracks");

            migrationBuilder.DropTable(
                name: "SpUsers");

            migrationBuilder.DropTable(
                name: "SpAlbums");

            migrationBuilder.DropTable(
                name: "SpArtists");
        }
    }
}
