using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class ChuyenDiscountPercentXuongChiTiet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "Promotions");

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "PromotionProducts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "PromotionProducts");

            migrationBuilder.AddColumn<int>(
                name: "DiscountPercent",
                table: "Promotions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
