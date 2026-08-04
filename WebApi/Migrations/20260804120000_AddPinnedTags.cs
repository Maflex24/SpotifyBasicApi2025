using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyBasicApi2025.Entities.Context;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260804120000_AddPinnedTags")]
    public partial class AddPinnedTags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPinned",
                table: "LibTags",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPinned",
                table: "LibTags");
        }
    }
}
