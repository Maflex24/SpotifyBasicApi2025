using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    /// <inheritdoc />
    public partial class AddLibrarySnapshotsAndAppSpotifyLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppUserSpotifyLinks",
                columns: table => new
                {
                    AppUserId = table.Column<long>(type: "bigint", nullable: false),
                    SpotifyUserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpotifyDisplayName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LinkedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUserSpotifyLinks", x => x.AppUserId);
                    table.ForeignKey(
                        name: "FK_AppUserSpotifyLinks_AppUsers_AppUserId",
                        column: x => x.AppUserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LibItemSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ItemType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpotifyItemId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ArtistsJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AlbumTitle = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OwnerName = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SpotifyUri = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CapturedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastConfirmedAtUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibItemSnapshots", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_AppUserSpotifyLinks_SpotifyUserId",
                table: "AppUserSpotifyLinks",
                column: "SpotifyUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LibItemSnapshots_UserId_ItemType_SpotifyItemId",
                table: "LibItemSnapshots",
                columns: new[] { "UserId", "ItemType", "SpotifyItemId" },
                unique: true);

            // Backfill only user-authored items. The catalog remains available during this
            // migration precisely so existing tags/notices retain a useful emergency label.
            migrationBuilder.Sql(@"
INSERT IGNORE INTO `LibItemSnapshots` (`UserId`,`ItemType`,`SpotifyItemId`,`Title`,`ArtistsJson`,`AlbumTitle`,`OwnerName`,`SpotifyUri`,`CapturedAtUtc`,`LastConfirmedAtUtc`)
SELECT x.`UserId`, 'artist', x.`SpotifyItemId`, COALESCE(NULLIF(a.`Name`,''),'[brak danych artysty]'),
       CASE WHEN a.`Name` IS NULL OR a.`Name` = '' THEN '[]' ELSE JSON_ARRAY(a.`Name`) END, NULL, NULL, a.`Uri`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM (SELECT `UserId`,`SpotifyItemId` FROM `LibTagAssigns` WHERE `ItemType`='artist' UNION SELECT `UserId`,`SpotifyItemId` FROM `LibItemNotes` WHERE `ItemType`='artist') x
LEFT JOIN `SpArtists` a ON a.`Id`=x.`SpotifyItemId`; ");
            migrationBuilder.Sql(@"
INSERT IGNORE INTO `LibItemSnapshots` (`UserId`,`ItemType`,`SpotifyItemId`,`Title`,`ArtistsJson`,`AlbumTitle`,`OwnerName`,`SpotifyUri`,`CapturedAtUtc`,`LastConfirmedAtUtc`)
SELECT x.`UserId`, 'album', x.`SpotifyItemId`, COALESCE(NULLIF(a.`Name`,''),'[brak danych albumu]'),
       COALESCE((SELECT JSON_ARRAYAGG(aa.`ArtistName`) FROM `SpAlbumArtistAssigns` aa WHERE aa.`AlbumId`=x.`SpotifyItemId`), '[]'), NULL, NULL, a.`Uri`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM (SELECT `UserId`,`SpotifyItemId` FROM `LibTagAssigns` WHERE `ItemType`='album' UNION SELECT `UserId`,`SpotifyItemId` FROM `LibItemNotes` WHERE `ItemType`='album') x
LEFT JOIN `SpAlbums` a ON a.`Id`=x.`SpotifyItemId`; ");
            migrationBuilder.Sql(@"
INSERT IGNORE INTO `LibItemSnapshots` (`UserId`,`ItemType`,`SpotifyItemId`,`Title`,`ArtistsJson`,`AlbumTitle`,`OwnerName`,`SpotifyUri`,`CapturedAtUtc`,`LastConfirmedAtUtc`)
SELECT x.`UserId`, 'track', x.`SpotifyItemId`, COALESCE(NULLIF(t.`Name`,''),'[brak danych utworu]'),
       COALESCE((SELECT JSON_ARRAYAGG(a.`Name`) FROM `SpTrackArtists` ta JOIN `SpArtists` a ON a.`Id`=ta.`ArtistId` WHERE ta.`TrackId`=x.`SpotifyItemId`), '[]'),
       al.`Name`, NULL, t.`Uri`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM (SELECT `UserId`,`SpotifyItemId` FROM `LibTagAssigns` WHERE `ItemType`='track') x
LEFT JOIN `SpTracks` t ON t.`Id`=x.`SpotifyItemId` LEFT JOIN `SpAlbums` al ON al.`Id`=t.`AlbumId`; ");
            migrationBuilder.Sql(@"
INSERT IGNORE INTO `LibItemSnapshots` (`UserId`,`ItemType`,`SpotifyItemId`,`Title`,`ArtistsJson`,`AlbumTitle`,`OwnerName`,`SpotifyUri`,`CapturedAtUtc`,`LastConfirmedAtUtc`)
SELECT x.`UserId`, 'playlist', x.`SpotifyItemId`, COALESCE(NULLIF(p.`Name`,''),'[brak danych playlisty]'), '[]', NULL, u.`DisplayName`, p.`Uri`, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6)
FROM (SELECT `UserId`,`SpotifyItemId` FROM `LibTagAssigns` WHERE `ItemType`='playlist') x
LEFT JOIN `SpPlaylists` p ON p.`Id`=x.`SpotifyItemId` LEFT JOIN `SpUsers` u ON u.`Id`=p.`OwnerId`; ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppUserSpotifyLinks");

            migrationBuilder.DropTable(
                name: "LibItemSnapshots");
        }
    }
}
