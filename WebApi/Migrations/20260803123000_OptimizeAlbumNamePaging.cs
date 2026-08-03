using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SpotifyBasicApi2025.Entities.Context;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260803123000_OptimizeAlbumNamePaging")]
    public partial class OptimizeAlbumNamePaging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AlbumName",
                table: "LibAlbumUserAssigns",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext");

            migrationBuilder.CreateIndex(
                name: "IX_LibAlbumUserAssigns_UserId_AlbumName_AlbumId",
                table: "LibAlbumUserAssigns",
                columns: new[] { "UserId", "AlbumName", "AlbumId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LibAlbumUserAssigns_UserId_AlbumName_AlbumId",
                table: "LibAlbumUserAssigns");

            migrationBuilder.AlterColumn<string>(
                name: "AlbumName",
                table: "LibAlbumUserAssigns",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)",
                oldMaxLength: 255);
        }
    }
}
