using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class XoaSessionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_AppUsers_UserId",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "CartItems");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CartItems",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_AppUsers_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CartItems_AppUsers_UserId",
                table: "CartItems");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "CartItems",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "SessionId",
                table: "CartItems",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CartItems_AppUsers_UserId",
                table: "CartItems",
                column: "UserId",
                principalTable: "AppUsers",
                principalColumn: "UserId");
        }
    }
}
