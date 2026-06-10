using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicCheck.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixTagRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Features_ProjectId",
                table: "Tags");

            migrationBuilder.CreateTable(
                name: "FeatureTags",
                columns: table => new
                {
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    TagsId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureTags", x => new { x.FeatureId, x.TagsId });
                    table.ForeignKey(
                        name: "FK_FeatureTags_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureTags_Tags_TagsId",
                        column: x => x.TagsId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureTags_TagsId",
                table: "FeatureTags",
                column: "TagsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Projects_ProjectId",
                table: "Tags",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Projects_ProjectId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "FeatureTags");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Features_ProjectId",
                table: "Tags",
                column: "ProjectId",
                principalTable: "Features",
                principalColumn: "Id");
        }
    }
}
