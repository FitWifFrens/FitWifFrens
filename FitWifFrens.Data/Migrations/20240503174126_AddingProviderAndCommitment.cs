using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingProviderAndCommitment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractAddress = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "CommittedUsers",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Transaction = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommittedUsers", x => new { x.CommitmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CommittedUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommittedUsers_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentProviders",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentProviders", x => new { x.CommitmentId, x.ProviderName });
                    table.ForeignKey(
                        name: "FK_CommitmentProviders_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitmentProviders_Providers_ProviderName",
                        column: x => x.ProviderName,
                        principalTable: "Providers",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Providers",
                column: "Name",
                values: new object[]
                {
                    "Strava",
                    "WorldId"
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins",
                column: "LoginProvider",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentProviders_ProviderName",
                table: "CommitmentProviders",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_CommittedUsers_UserId",
                table: "CommittedUsers",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUserLogins_Providers_LoginProvider",
                table: "AspNetUserLogins",
                column: "LoginProvider",
                principalTable: "Providers",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUserLogins_Providers_LoginProvider",
                table: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "CommitmentProviders");

            migrationBuilder.DropTable(
                name: "CommittedUsers");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Commitments");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUserLogins_LoginProvider",
                table: "AspNetUserLogins");
        }
    }
}
