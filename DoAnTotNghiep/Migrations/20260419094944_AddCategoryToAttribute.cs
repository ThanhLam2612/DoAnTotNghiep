using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToAttribute : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "ProductAttributes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductAttributes_CategoryId",
                table: "ProductAttributes",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductAttributes_Categories_CategoryId",
                table: "ProductAttributes",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductAttributes_Categories_CategoryId",
                table: "ProductAttributes");

            migrationBuilder.DropIndex(
                name: "IX_ProductAttributes_CategoryId",
                table: "ProductAttributes");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "ProductAttributes");
        }
    }
}
