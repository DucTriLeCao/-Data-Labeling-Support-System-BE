using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataLabeling.Domain.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnotationCoordinateFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add annotation_type column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "annotation_type",
                table: "annotations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            // Add coordinate_data column if it doesn't exist
            migrationBuilder.AddColumn<string>(
                name: "coordinate_data",
                table: "annotations",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "annotation_type",
                table: "annotations");

            migrationBuilder.DropColumn(
                name: "coordinate_data",
                table: "annotations");
        }
    }
}
