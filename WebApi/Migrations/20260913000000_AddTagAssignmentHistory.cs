using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyBasicApi2025.Entities.Context;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260913000000_AddTagAssignmentHistory")]
    public partial class AddTagAssignmentHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibTagAssignmentHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    TagName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    TagGroupName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    ItemType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    SpotifyItemId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    ItemName = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    PreviousStrength = table.Column<float>(type: "float", nullable: true),
                    NewStrength = table.Column<float>(type: "float", nullable: true),
                    ChangedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_LibTagAssignmentHistories", x => x.Id))
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAssignmentHistories_UserId_ChangedAt",
                table: "LibTagAssignmentHistories",
                columns: new[] { "UserId", "ChangedAt" });

            migrationBuilder.Sql(@"
                INSERT INTO `LibTagAssignmentHistories`
                    (`UserId`, `Action`, `TagId`, `TagName`, `TagGroupName`, `ItemType`, `SpotifyItemId`, `ItemName`, `PreviousStrength`, `NewStrength`, `ChangedAt`)
                SELECT a.`UserId`, 'added', a.`TagId`, t.`Name`, g.`Name`, a.`ItemType`, a.`SpotifyItemId`,
                    CASE a.`ItemType`
                        WHEN 'artist' THEN COALESCE(artist.`Name`, a.`SpotifyItemId`)
                        WHEN 'album' THEN COALESCE(album.`Name`, a.`SpotifyItemId`)
                        WHEN 'track' THEN COALESCE(track.`Name`, a.`SpotifyItemId`)
                        WHEN 'playlist' THEN COALESCE(playlist.`Name`, a.`SpotifyItemId`)
                        ELSE a.`SpotifyItemId`
                    END,
                    NULL, a.`Strength`, a.`CreatedAt`
                FROM `LibTagAssigns` a
                INNER JOIN `LibTags` t ON t.`Id` = a.`TagId`
                INNER JOIN `LibTagGroups` g ON g.`Id` = t.`TagGroupId`
                LEFT JOIN `SpArtists` artist ON a.`ItemType` = 'artist' AND artist.`Id` = a.`SpotifyItemId`
                LEFT JOIN `SpAlbums` album ON a.`ItemType` = 'album' AND album.`Id` = a.`SpotifyItemId`
                LEFT JOIN `SpTracks` track ON a.`ItemType` = 'track' AND track.`Id` = a.`SpotifyItemId`
                LEFT JOIN `SpPlaylists` playlist ON a.`ItemType` = 'playlist' AND playlist.`Id` = a.`SpotifyItemId`;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LibTagAssignmentHistories");
        }
    }
}
