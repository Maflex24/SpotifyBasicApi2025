using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddLibArtistUserAssign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibArtistUserAssigns",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateCreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibArtistUserAssigns", x => new { x.UserId, x.ArtistId });
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

            migrationBuilder.CreateIndex(
                name: "IX_LibArtistUserAssigns_ArtistId",
                table: "LibArtistUserAssigns",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_LibArtistUserAssigns_UserId",
                table: "LibArtistUserAssigns",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibArtistUserAssigns");
        }
    }
}
