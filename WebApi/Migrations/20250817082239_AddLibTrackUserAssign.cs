using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddLibTrackUserAssign : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibTrackUserAssigns");
        }
    }
}
