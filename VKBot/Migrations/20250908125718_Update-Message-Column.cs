using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderAt",
                table: "MessageDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAt",
                table: "MessageDeliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "MessageDeliveries",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastReminderAt",
                table: "MessageDeliveries");

            migrationBuilder.DropColumn(
                name: "NextRetryAt",
                table: "MessageDeliveries");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "MessageDeliveries");
        }
    }
}
