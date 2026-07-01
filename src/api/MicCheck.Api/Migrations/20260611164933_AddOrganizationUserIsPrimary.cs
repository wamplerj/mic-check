using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicCheck.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationUserIsPrimary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "OrganizationUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "OrganizationUsers");
        }
    }
}
