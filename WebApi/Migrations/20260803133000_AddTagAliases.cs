using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using SpotifyBasicApi2025.Entities.Context;

#nullable disable

namespace SpotifyBasicApi2025.Migrations
{
    [DbContext(typeof(DatabaseContext))]
    [Migration("20260803133000_AddTagAliases")]
    public partial class AddTagAliases : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LibTagAliases",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TagId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    NormalizedName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LibTagAliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LibTagAliases_LibTags_TagId",
                        column: x => x.TagId,
                        principalTable: "LibTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAliases_TagId",
                table: "LibTagAliases",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_LibTagAliases_UserId_NormalizedName",
                table: "LibTagAliases",
                columns: new[] { "UserId", "NormalizedName" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "LibTagAliases");
        }
    }
}
