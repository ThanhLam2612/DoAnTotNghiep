using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class SuaBienThe1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantAttributeValues_ProductAttributes_AttributeId",
                table: "VariantAttributeValues");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "VariantAttributeValues");

            migrationBuilder.DropColumn(
                name: "Value",
                table: "VariantAttributeValues");

            migrationBuilder.DropColumn(
                name: "SKU",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "BasePrice",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "AttributeId",
                table: "VariantAttributeValues",
                newName: "PredefinedValueId");

            migrationBuilder.RenameIndex(
                name: "IX_VariantAttributeValues_AttributeId",
                table: "VariantAttributeValues",
                newName: "IX_VariantAttributeValues_PredefinedValueId");

            migrationBuilder.RenameColumn(
                name: "AdditionalPrice",
                table: "ProductVariants",
                newName: "Price");

            migrationBuilder.CreateTable(
                name: "PredefinedAttributeValues",
                columns: table => new
                {
                    PredefinedValueId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttributeId = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredefinedAttributeValues", x => x.PredefinedValueId);
                    table.ForeignKey(
                        name: "FK_PredefinedAttributeValues_ProductAttributes_AttributeId",
                        column: x => x.AttributeId,
                        principalTable: "ProductAttributes",
                        principalColumn: "AttributeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    VariantId = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_ProductImages_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "VariantId");
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductSpecifications",
                columns: table => new
                {
                    SpecId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SpecName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SpecValue = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductSpecifications", x => x.SpecId);
                    table.ForeignKey(
                        name: "FK_ProductSpecifications_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "ProductId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredefinedAttributeValues_AttributeId",
                table: "PredefinedAttributeValues",
                column: "AttributeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_VariantId",
                table: "ProductImages",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductSpecifications_ProductId",
                table: "ProductSpecifications",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_VariantAttributeValues_PredefinedAttributeValues_PredefinedValueId",
                table: "VariantAttributeValues",
                column: "PredefinedValueId",
                principalTable: "PredefinedAttributeValues",
                principalColumn: "PredefinedValueId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_VariantAttributeValues_PredefinedAttributeValues_PredefinedValueId",
                table: "VariantAttributeValues");

            migrationBuilder.DropTable(
                name: "PredefinedAttributeValues");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "ProductSpecifications");

            migrationBuilder.RenameColumn(
                name: "PredefinedValueId",
                table: "VariantAttributeValues",
                newName: "AttributeId");

            migrationBuilder.RenameIndex(
                name: "IX_VariantAttributeValues_PredefinedValueId",
                table: "VariantAttributeValues",
                newName: "IX_VariantAttributeValues_AttributeId");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "ProductVariants",
                newName: "AdditionalPrice");

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "VariantAttributeValues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Value",
                table: "VariantAttributeValues",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SKU",
                table: "ProductVariants",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "BasePrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantAttributeValues_ProductAttributes_AttributeId",
                table: "VariantAttributeValues",
                column: "AttributeId",
                principalTable: "ProductAttributes",
                principalColumn: "AttributeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
