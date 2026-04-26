using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class UpdateReview1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantVariantId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VariantId",
                table: "Reviews",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductVariantVariantId",
                table: "Reviews",
                column: "ProductVariantVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_ProductVariants_ProductVariantVariantId",
                table: "Reviews",
                column: "ProductVariantVariantId",
                principalTable: "ProductVariants",
                principalColumn: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_ProductVariants_ProductVariantVariantId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_ProductVariantVariantId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "ProductVariantVariantId",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "Reviews");
        }
    }
}
