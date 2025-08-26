using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    /// <inheritdoc />
    public partial class MySqlInit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpArtists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Href = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Genres = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    Images = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpArtists", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Href = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Product = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Images = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Followers = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpUsers", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpAlbums",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Href = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalTracks = table.Column<int>(type: "int", nullable: false),
                    AvailableMarkets = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReleaseDate = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReleaseDatePrecision = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Images = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    Label = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SpArtistId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpAlbums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpAlbums_SpArtists_SpArtistId",
                        column: x => x.SpArtistId,
                        principalTable: "SpArtists",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibArtistUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibPlaylistFolders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibTagGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ForAlbum = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForArtist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForTrack = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForPlaylist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTagGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTagGroups_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibAlbumUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpAlbumArtistAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AlbumId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlbumName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpSavedAlbums",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpTracks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Href = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    Explicit = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DiscNumber = table.Column<int>(type: "int", nullable: false),
                    TrackNumber = table.Column<int>(type: "int", nullable: false),
                    AvailableMarkets = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Popularity = table.Column<int>(type: "int", nullable: false),
                    IsPlayable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsLocal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PreviewUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Images = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AlbumId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpPlaylists",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Uri = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Href = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Collaborative = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Public = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TotalTracks = table.Column<int>(type: "int", nullable: false),
                    Images = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SnapshotId = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FolderId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OwnerId = table.Column<string>(type: "varchar(255)", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibTags",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ForAlbum = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForArtist = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForTrack = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ForPlaylist = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibTrackUserAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackName = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpSavedTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpTrackArtists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TrackId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibPlaylistFolderAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PlaylistId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FolderId = table.Column<long>(type: "bigint", nullable: true),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SpPlaylistTracks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PlaylistId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrackId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibTagAssigns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Strength = table.Column<float>(type: "float", nullable: false),
                    ItemType = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpotifyItemId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
