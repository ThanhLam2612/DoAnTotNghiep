using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class ThemReviewLike : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLike_Reviews_ReviewId",
                table: "ReviewLike");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLike",
                table: "ReviewLike");

            migrationBuilder.RenameTable(
                name: "ReviewLike",
                newName: "ReviewLikes");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLike_ReviewId",
                table: "ReviewLikes",
                newName: "IX_ReviewLikes_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLikes",
                table: "ReviewLikes",
                column: "ReviewLikeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLikes_Reviews_ReviewId",
                table: "ReviewLikes",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "ReviewId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ReviewLikes_Reviews_ReviewId",
                table: "ReviewLikes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ReviewLikes",
                table: "ReviewLikes");

            migrationBuilder.RenameTable(
                name: "ReviewLikes",
                newName: "ReviewLike");

            migrationBuilder.RenameIndex(
                name: "IX_ReviewLikes_ReviewId",
                table: "ReviewLike",
                newName: "IX_ReviewLike_ReviewId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ReviewLike",
                table: "ReviewLike",
                column: "ReviewLikeId");

            migrationBuilder.AddForeignKey(
                name: "FK_ReviewLike_Reviews_ReviewId",
                table: "ReviewLike",
                column: "ReviewId",
                principalTable: "Reviews",
                principalColumn: "ReviewId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
