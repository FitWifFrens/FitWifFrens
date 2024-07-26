using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingUserDisplay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Displays",
                columns: table => new
                {
                    MacAddress = table.Column<string>(type: "character(17)", fixedLength: true, maxLength: 17, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Displays", x => x.MacAddress);
                });

            migrationBuilder.CreateTable(
                name: "UserDisplays",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    MacAddress = table.Column<string>(type: "character(17)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDisplays", x => new { x.UserId, x.MacAddress });
                    table.ForeignKey(
                        name: "FK_UserDisplays_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserDisplays_Displays_MacAddress",
                        column: x => x.MacAddress,
                        principalTable: "Displays",
                        principalColumn: "MacAddress",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDisplays_MacAddress",
                table: "UserDisplays",
                column: "MacAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDisplays");

            migrationBuilder.DropTable(
                name: "Displays");
        }
    }
}
