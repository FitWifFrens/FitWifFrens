using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixingProviderLogins : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins",
                column: "LoginProvider",
                unique: true);
        }
    }
}
