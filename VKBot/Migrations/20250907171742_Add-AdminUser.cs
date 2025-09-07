using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VKBot.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "GroupId",
                table: "Users",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "VkUserId", "FullName", "GroupId", "IsBlocked", "IsConfirmed", "Role" },
                values: new object[,]
                {
                    { 562436407L, "Admin 2", null, false, true, "Admin" },
                    { 651565729L, "Admin 1", null, false, true, "Admin" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "VkUserId",
                keyValue: 562436407L);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "VkUserId",
                keyValue: 651565729L);

            migrationBuilder.AlterColumn<long>(
                name: "GroupId",
                table: "Users",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
