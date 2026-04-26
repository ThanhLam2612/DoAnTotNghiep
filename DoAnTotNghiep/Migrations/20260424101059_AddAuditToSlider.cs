using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnTotNghiep.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditToSlider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LinkUrl",
                table: "Sliders");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Sliders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Sliders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UpdatedBy",
                table: "Sliders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Sliders");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Sliders");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Sliders");

            migrationBuilder.AddColumn<string>(
                name: "LinkUrl",
                table: "Sliders",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
