using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VKBot.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMessageEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageDeliveries_Users_UserId",
                table: "MessageDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_RecipientId",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "MessageGroups");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageDeliveries",
                table: "MessageDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_MessageDeliveries_UserId",
                table: "MessageDeliveries");

            migrationBuilder.DropColumn(
                name: "MessageType",
                table: "Messages");

            migrationBuilder.RenameColumn(
                name: "TagerGroup",
                table: "Messages",
                newName: "Payload");

            migrationBuilder.RenameColumn(
                name: "RecipientId",
                table: "Messages",
                newName: "UserVkUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_RecipientId",
                table: "Messages",
                newName: "IX_Messages_UserVkUserId");

            migrationBuilder.RenameColumn(
                name: "BlockedUntil",
                table: "MessageDeliveries",
                newName: "DispatchTime");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "MessageDeliveries",
                newName: "Id");

            migrationBuilder.AddColumn<bool>(
                name: "EnableReminder",
                table: "Messages",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReminderSent",
                table: "Messages",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReplyToMessageId",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RecipientId",
                table: "MessageDeliveries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "isRead",
                table: "MessageDeliveries",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageDeliveries",
                table: "MessageDeliveries",
                columns: new[] { "MessageId", "RecipientId" });

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_RecipientId",
                table: "MessageDeliveries",
                column: "RecipientId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageDeliveries_Users_RecipientId",
                table: "MessageDeliveries",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "VkUserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_UserVkUserId",
                table: "Messages",
                column: "UserVkUserId",
                principalTable: "Users",
                principalColumn: "VkUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MessageDeliveries_Users_RecipientId",
                table: "MessageDeliveries");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_UserVkUserId",
                table: "Messages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MessageDeliveries",
                table: "MessageDeliveries");

            migrationBuilder.DropIndex(
                name: "IX_MessageDeliveries_RecipientId",
                table: "MessageDeliveries");

            migrationBuilder.DropColumn(
                name: "EnableReminder",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "LastReminderSent",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ReplyToMessageId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "RecipientId",
                table: "MessageDeliveries");

            migrationBuilder.DropColumn(
                name: "isRead",
                table: "MessageDeliveries");

            migrationBuilder.RenameColumn(
                name: "UserVkUserId",
                table: "Messages",
                newName: "RecipientId");

            migrationBuilder.RenameColumn(
                name: "Payload",
                table: "Messages",
                newName: "TagerGroup");

            migrationBuilder.RenameIndex(
                name: "IX_Messages_UserVkUserId",
                table: "Messages",
                newName: "IX_Messages_RecipientId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "MessageDeliveries",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "DispatchTime",
                table: "MessageDeliveries",
                newName: "BlockedUntil");

            migrationBuilder.AddColumn<string>(
                name: "MessageType",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MessageDeliveries",
                table: "MessageDeliveries",
                columns: new[] { "MessageId", "UserId" });

            migrationBuilder.CreateTable(
                name: "MessageGroups",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false),
                    GroupId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageGroups", x => new { x.MessageId, x.GroupId });
                    table.ForeignKey(
                        name: "FK_MessageGroups_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageGroups_Messages_MessageId",
                        column: x => x.MessageId,
                        principalTable: "Messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessageDeliveries_UserId",
                table: "MessageDeliveries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessageGroups_GroupId",
                table: "MessageGroups",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_MessageDeliveries_Users_UserId",
                table: "MessageDeliveries",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "VkUserId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Users_RecipientId",
                table: "Messages",
                column: "RecipientId",
                principalTable: "Users",
                principalColumn: "VkUserId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
