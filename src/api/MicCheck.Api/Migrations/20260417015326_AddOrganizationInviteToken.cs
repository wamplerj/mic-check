using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicCheck.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationInviteToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InviteToken",
                table: "Organizations",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_InviteToken",
                table: "Organizations",
                column: "InviteToken",
                unique: true,
                filter: "\"InviteToken\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Organizations_InviteToken",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "InviteToken",
                table: "Organizations");
        }
    }
}
