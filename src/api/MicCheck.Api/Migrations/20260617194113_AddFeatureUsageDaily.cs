using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MicCheck.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureUsageDaily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureUsageDaily",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EnvironmentId = table.Column<int>(type: "integer", nullable: false),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    FeatureName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UsageDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Count = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureUsageDaily", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureUsageDaily_EnvironmentId_FeatureId_UsageDate",
                table: "FeatureUsageDaily",
                columns: new[] { "EnvironmentId", "FeatureId", "UsageDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureUsageDaily_EnvironmentId_UsageDate",
                table: "FeatureUsageDaily",
                columns: new[] { "EnvironmentId", "UsageDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureUsageDaily");
        }
    }
}
