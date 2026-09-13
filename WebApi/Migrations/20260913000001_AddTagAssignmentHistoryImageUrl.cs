using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyBasicApi2025.Entities.Context;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260913000001_AddTagAssignmentHistoryImageUrl")]
    public partial class AddTagAssignmentHistoryImageUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration may resume after MySQL has already committed the DDL while a
            // later command failed. MySQL versions used on VPS do not all support
            // ADD COLUMN IF NOT EXISTS, therefore check INFORMATION_SCHEMA and execute
            // the DDL dynamically. The commands share EF's migration connection.
            migrationBuilder.Sql(@"
                SET @image_url_column_exists = (
                    SELECT COUNT(*)
                    FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                      AND TABLE_NAME = 'LibTagAssignmentHistories'
                      AND COLUMN_NAME = 'ImageUrl'
                );");
            migrationBuilder.Sql(@"
                SET @add_image_url_column_sql = IF(
                    @image_url_column_exists = 0,
                    'ALTER TABLE `LibTagAssignmentHistories` ADD COLUMN `ImageUrl` longtext NULL',
                    'SELECT 1'
                );");
            migrationBuilder.Sql("PREPARE add_image_url_column_statement FROM @add_image_url_column_sql;");
            migrationBuilder.Sql("EXECUTE add_image_url_column_statement;");
            migrationBuilder.Sql("DEALLOCATE PREPARE add_image_url_column_statement;");

            // Spotify returns images from the largest to the smallest, so prefer the third
            // entry (usually 64px) and fall back through the available collection. Legacy
            // non-JSON values deliberately remain empty rather than risking a malformed URL.
            migrationBuilder.Sql(@"
                UPDATE `LibTagAssignmentHistories` AS history
                INNER JOIN `SpArtists` AS artist ON artist.`Id` = history.`SpotifyItemId` COLLATE utf8mb4_unicode_ci
                SET history.`ImageUrl` = CASE WHEN JSON_VALID(artist.`Images`) THEN COALESCE(JSON_UNQUOTE(JSON_EXTRACT(artist.`Images`, '$[2].url')), JSON_UNQUOTE(JSON_EXTRACT(artist.`Images`, '$[1].url')), JSON_UNQUOTE(JSON_EXTRACT(artist.`Images`, '$[0].url'))) ELSE NULL END
                WHERE history.`ItemType` = 'artist' AND history.`ImageUrl` IS NULL;");

            migrationBuilder.Sql(@"
                UPDATE `LibTagAssignmentHistories` AS history
                INNER JOIN `SpAlbums` AS album ON album.`Id` = history.`SpotifyItemId` COLLATE utf8mb4_unicode_ci
                SET history.`ImageUrl` = CASE WHEN JSON_VALID(album.`Images`) THEN COALESCE(JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[2].url')), JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[1].url')), JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[0].url'))) ELSE NULL END
                WHERE history.`ItemType` = 'album' AND history.`ImageUrl` IS NULL;");

            migrationBuilder.Sql(@"
                UPDATE `LibTagAssignmentHistories` AS history
                SET history.`ImageUrl` = (
                    SELECT COALESCE(
                        CASE WHEN JSON_VALID(album.`Images`) THEN COALESCE(JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[2].url')), JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[1].url')), JSON_UNQUOTE(JSON_EXTRACT(album.`Images`, '$[0].url'))) ELSE NULL END,
                        CASE WHEN JSON_VALID(track.`Images`) THEN COALESCE(JSON_UNQUOTE(JSON_EXTRACT(track.`Images`, '$[2].url')), JSON_UNQUOTE(JSON_EXTRACT(track.`Images`, '$[1].url')), JSON_UNQUOTE(JSON_EXTRACT(track.`Images`, '$[0].url'))) ELSE NULL END)
                    FROM `SpTracks` AS track
                    LEFT JOIN `SpAlbums` AS album ON album.`Id` = track.`AlbumId`
                    WHERE track.`Id` = history.`SpotifyItemId` COLLATE utf8mb4_unicode_ci
                )
                WHERE history.`ItemType` = 'track' AND history.`ImageUrl` IS NULL;");

            migrationBuilder.Sql(@"
                UPDATE `LibTagAssignmentHistories` AS history
                INNER JOIN `SpPlaylists` AS playlist ON playlist.`Id` = history.`SpotifyItemId` COLLATE utf8mb4_unicode_ci
                SET history.`ImageUrl` = CASE WHEN JSON_VALID(playlist.`Images`) THEN COALESCE(JSON_UNQUOTE(JSON_EXTRACT(playlist.`Images`, '$[2].url')), JSON_UNQUOTE(JSON_EXTRACT(playlist.`Images`, '$[1].url')), JSON_UNQUOTE(JSON_EXTRACT(playlist.`Images`, '$[0].url'))) ELSE NULL END
                WHERE history.`ItemType` = 'playlist' AND history.`ImageUrl` IS NULL;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ImageUrl", table: "LibTagAssignmentHistories");
        }
    }
}
