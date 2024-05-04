using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingCommitmentComplete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistributedAmount",
                table: "CommittedUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Complete",
                table: "Commitments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistributedAmount",
                table: "CommittedUsers");

            migrationBuilder.DropColumn(
                name: "Complete",
                table: "Commitments");
        }
    }
}
