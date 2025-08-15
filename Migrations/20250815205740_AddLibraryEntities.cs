using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddLibraryEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibTagGroups_SpUsers_UserId",
                table: "LibTagGroups");

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
                        name: "FK_LibTagAssigns_SpAlbums_SpotifyItemId",
                        column: x => x.SpotifyItemId,
                        principalTable: "SpAlbums",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_SpArtists_SpotifyItemId",
                        column: x => x.SpotifyItemId,
                        principalTable: "SpArtists",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_SpPlaylists_SpotifyItemId",
                        column: x => x.SpotifyItemId,
                        principalTable: "SpPlaylists",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_SpTracks_SpotifyItemId",
                        column: x => x.SpotifyItemId,
                        principalTable: "SpTracks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LibTagAssigns_SpUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "SpUsers",
                        principalColumn: "Id");
                });

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

            migrationBuilder.AddForeignKey(
                name: "FK_LibTagGroups_SpUsers_UserId",
                table: "LibTagGroups",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LibTagGroups_SpUsers_UserId",
                table: "LibTagGroups");

            migrationBuilder.DropTable(
                name: "LibTagAssigns");

            migrationBuilder.AddForeignKey(
                name: "FK_LibTagGroups_SpUsers_UserId",
                table: "LibTagGroups",
                column: "UserId",
                principalTable: "SpUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
