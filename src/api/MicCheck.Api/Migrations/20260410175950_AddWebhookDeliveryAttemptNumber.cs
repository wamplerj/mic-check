using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MicCheck.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookDeliveryAttemptNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "WebhookDeliveryLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "WebhookDeliveryLogs");
        }
    }
}
