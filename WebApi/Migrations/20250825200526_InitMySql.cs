using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    /// <inheritdoc />
    public partial class InitMySql : Migration
    {
        /// <inheritdoc />
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
                    SpArtistId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpAlbums_SpArtists_SpArtistId",
                        column: x => x.SpArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LibArtistUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibArtistUserAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibArtistUserAssigns_SpArtists_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibArtistUserAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibPlaylistFolders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
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
                    table.ForeignKey(
                        name: "FK_LibPlaylistFolders_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LibTagGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ForAlbum = table.Column<bool>(type: "bit", nullable: false),
                    ForArtist = table.Column<bool>(type: "bit", nullable: false),
                    ForTrack = table.Column<bool>(type: "bit", nullable: false),
                    ForPlaylist = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTagGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTagGroups_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "LibAlbumUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlbumName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibAlbumUserAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibAlbumUserAssigns_SpAlbums_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "SpAlbums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibAlbumUserAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpAlbumArtistAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlbumName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: false)
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
                    FolderId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OwnerId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpPlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpPlaylists_LibPlaylistFolders_FolderId",
                        column: x => x.FolderId,
                        principalTable: "LibPlaylistFolders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SpPlaylists_SpUsers_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "SpUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LibTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ForAlbum = table.Column<bool>(type: "bit", nullable: false),
                    ForArtist = table.Column<bool>(type: "bit", nullable: false),
                    ForTrack = table.Column<bool>(type: "bit", nullable: false),
                    ForPlaylist = table.Column<bool>(type: "bit", nullable: false),
                    TagGroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTags_LibTagGroups_TagGroupId",
                        column: x => x.TagGroupId,
                        principalTable: "LibTagGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LibTrackUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrackId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrackName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTrackUserAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTrackUserAssigns_SpTracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "SpTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibTrackUserAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
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
                name: "LibTagAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Strength = table.Column<float>(type: "real", nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SpotifyItemId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTagAssigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_LibTags_TagId",
                        column: x => x.TagId,
                        principalTable: "LibTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibAlbumUserAssigns_AlbumId",
                table: "LibAlbumUserAssigns",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_LibAlbumUserAssigns_UserId",
                table: "LibAlbumUserAssigns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibAlbumUserAssigns_UserId_AlbumId",
                table: "LibAlbumUserAssigns",
                columns: new[] { "UserId", "AlbumId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibArtistUserAssigns_ArtistId",
                table: "LibArtistUserAssigns",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibArtistUserAssigns_UserId",
                table: "LibArtistUserAssigns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibArtistUserAssigns_UserId_ArtistId",
                table: "LibArtistUserAssigns",
                columns: new[] { "UserId", "ArtistId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_ParentId",
                table: "LibPlaylistFolders",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_LibPlaylistFolders_UserId",
                table: "LibPlaylistFolders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssigns_ItemType",
                table: "LibTagAssigns",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssigns_SpotifyItemId",
                table: "LibTagAssigns",
                column: "SpotifyItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssigns_TagId",
                table: "LibTagAssigns",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssigns_UserId",
                table: "LibTagAssigns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssigns_UserId_TagId_SpotifyItemId_ItemType",
                table: "LibTagAssigns",
                columns: new[] { "UserId", "TagId", "SpotifyItemId", "ItemType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibTagGroups_Name",
                table: "LibTagGroups",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagGroups_UserId",
                table: "LibTagGroups",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTags_Name",
                table: "LibTags",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_LibTags_TagGroupId",
                table: "LibTags",
                column: "TagGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTrackUserAssigns_TrackId",
                table: "LibTrackUserAssigns",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTrackUserAssigns_UserId",
                table: "LibTrackUserAssigns",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTrackUserAssigns_UserId_TrackId",
                table: "LibTrackUserAssigns",
                columns: new[] { "UserId", "TrackId" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbums_Name",
                table: "SpAlbums",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpAlbums_SpArtistId",
                table: "SpAlbums",
                column: "SpArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_SpArtists_Name",
                table: "SpArtists",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_SpPlaylists_FolderId",
                table: "SpPlaylists",
                column: "FolderId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibAlbumUserAssigns");

            migrationBuilder.DropTable(
                name: "LibArtistUserAssigns");

            migrationBuilder.DropTable(
                name: "LibPlaylistFolderAssigns");

            migrationBuilder.DropTable(
                name: "LibTagAssigns");

            migrationBuilder.DropTable(
                name: "LibTrackUserAssigns");

            migrationBuilder.DropTable(
                name: "SpAlbumArtistAssigns");

            migrationBuilder.DropTable(
                name: "SpPlaylistTracks");

            migrationBuilder.DropTable(
                name: "SpSavedAlbums");

            migrationBuilder.DropTable(
                name: "SpSavedTracks");

            migrationBuilder.DropTable(
                name: "SpTrackArtists");

            migrationBuilder.DropTable(
                name: "LibTags");

            migrationBuilder.DropTable(
                name: "SpPlaylists");

            migrationBuilder.DropTable(
                name: "SpTracks");

            migrationBuilder.DropTable(
                name: "LibTagGroups");

            migrationBuilder.DropTable(
                name: "LibPlaylistFolders");

            migrationBuilder.DropTable(
                name: "SpAlbums");

            migrationBuilder.DropTable(
                name: "SpUsers");

            migrationBuilder.DropTable(
                name: "SpArtists");
        }
    }
}
