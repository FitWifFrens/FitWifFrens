using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingCommitmentDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Amount",
                table: "Commitments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Commitments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Commitments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Commitments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Commitments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Commitments");
        }
    }
}
