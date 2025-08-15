using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    public partial class AddLibTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LibTags");

            migrationBuilder.DropTable(
                name: "LibTagGroups");
        }
    }
}
