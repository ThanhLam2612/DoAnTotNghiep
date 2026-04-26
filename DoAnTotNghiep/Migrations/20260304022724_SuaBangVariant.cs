using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class SuaBangVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OriginalPrice",
                table: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ProductVariants",
                newName: "AdditionalPrice");

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SKU",
                table: "ProductVariants");

            migrationBuilder.RenameColumn(
                name: "AdditionalPrice",
                table: "ProductVariants",
                newName: "Price");

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrice",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
