using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Commitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Image = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    ContractAddress = table.Column<string>(type: "text", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commitments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Metrics",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Metrics", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deposits",
                columns: table => new
                {
                    Transaction = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deposits", x => x.Transaction);
                    table.ForeignKey(
                        name: "FK_Deposits_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentPeriods",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentPeriods", x => new { x.CommitmentId, x.StartDate, x.EndDate });
                    table.ForeignKey(
                        name: "FK_CommitmentPeriods_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentUsers",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Stake = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentUsers", x => new { x.CommitmentId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CommitmentUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitmentUsers_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MetricValues",
                columns: table => new
                {
                    MetricName = table.Column<string>(type: "character varying(256)", nullable: false),
                    Type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetricValues", x => new { x.MetricName, x.Type });
                    table.ForeignKey(
                        name: "FK_MetricValues_Metrics_MetricName",
                        column: x => x.MetricName,
                        principalTable: "Metrics",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "character varying(256)", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_Providers_LoginProvider",
                        column: x => x.LoginProvider,
                        principalTable: "Providers",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentPeriodUsers",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Stake = table.Column<decimal>(type: "numeric", nullable: false),
                    Reward = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentPeriodUsers", x => new { x.CommitmentId, x.StartDate, x.EndDate, x.UserId });
                    table.ForeignKey(
                        name: "FK_CommitmentPeriodUsers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitmentPeriodUsers_CommitmentPeriods_CommitmentId_StartD~",
                        columns: x => new { x.CommitmentId, x.StartDate, x.EndDate },
                        principalTable: "CommitmentPeriods",
                        principalColumns: new[] { "CommitmentId", "StartDate", "EndDate" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderMetricValues",
                columns: table => new
                {
                    ProviderName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderMetricValues", x => new { x.ProviderName, x.MetricName, x.MetricType });
                    table.ForeignKey(
                        name: "FK_ProviderMetricValues_MetricValues_MetricName_MetricType",
                        columns: x => new { x.MetricName, x.MetricType },
                        principalTable: "MetricValues",
                        principalColumns: new[] { "MetricName", "Type" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProviderMetricValues_Providers_ProviderName",
                        column: x => x.ProviderName,
                        principalTable: "Providers",
                        principalColumn: "Name",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Goals",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Rule = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Goals", x => new { x.CommitmentId, x.ProviderName, x.MetricName, x.MetricType });
                    table.ForeignKey(
                        name: "FK_Goals_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Goals_ProviderMetricValues_ProviderName_MetricName_MetricTy~",
                        columns: x => new { x.ProviderName, x.MetricName, x.MetricType },
                        principalTable: "ProviderMetricValues",
                        principalColumns: new[] { "ProviderName", "MetricName", "MetricType" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserProviderMetricValues",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserProviderMetricValues", x => new { x.UserId, x.ProviderName, x.MetricName, x.MetricType, x.Time });
                    table.ForeignKey(
                        name: "FK_UserProviderMetricValues_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserProviderMetricValues_ProviderMetricValues_ProviderName_~",
                        columns: x => new { x.ProviderName, x.MetricName, x.MetricType },
                        principalTable: "ProviderMetricValues",
                        principalColumns: new[] { "ProviderName", "MetricName", "MetricType" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentPeriodUserGoals",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricName = table.Column<string>(type: "character varying(256)", nullable: false),
                    MetricType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentPeriodUserGoals", x => new { x.CommitmentId, x.StartDate, x.EndDate, x.UserId, x.ProviderName, x.MetricName, x.MetricType });
                    table.ForeignKey(
                        name: "FK_CommitmentPeriodUserGoals_CommitmentPeriodUsers_CommitmentI~",
                        columns: x => new { x.CommitmentId, x.StartDate, x.EndDate, x.UserId },
                        principalTable: "CommitmentPeriodUsers",
                        principalColumns: new[] { "CommitmentId", "StartDate", "EndDate", "UserId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CommitmentPeriodUserGoals_Goals_CommitmentId_ProviderName_M~",
                        columns: x => new { x.CommitmentId, x.ProviderName, x.MetricName, x.MetricType },
                        principalTable: "Goals",
                        principalColumns: new[] { "CommitmentId", "ProviderName", "MetricName", "MetricType" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Metrics",
                column: "Name",
                values: new object[]
                {
                    "Exercise",
                    "Running",
                    "Weight"
                });

            migrationBuilder.InsertData(
                table: "Providers",
                column: "Name",
                values: new object[]
                {
                    "Strava",
                    "Withings"
                });

            migrationBuilder.InsertData(
                table: "MetricValues",
                columns: new[] { "MetricName", "Type" },
                values: new object[,]
                {
                    { "Exercise", "Count" },
                    { "Exercise", "Minutes" },
                    { "Running", "Count" },
                    { "Running", "Minutes" },
                    { "Weight", "Value" }
                });

            migrationBuilder.InsertData(
                table: "ProviderMetricValues",
                columns: new[] { "MetricName", "MetricType", "ProviderName" },
                values: new object[,]
                {
                    { "Exercise", "Count", "Strava" },
                    { "Exercise", "Minutes", "Strava" },
                    { "Running", "Count", "Strava" },
                    { "Running", "Minutes", "Strava" },
                    { "Weight", "Value", "Withings" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentPeriodUserGoals_CommitmentId_ProviderName_MetricN~",
                table: "CommitmentPeriodUserGoals",
                columns: new[] { "CommitmentId", "ProviderName", "MetricName", "MetricType" });

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentPeriodUsers_UserId",
                table: "CommitmentPeriodUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentUsers_UserId",
                table: "CommitmentUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deposits_UserId",
                table: "Deposits",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Goals_ProviderName_MetricName_MetricType",
                table: "Goals",
                columns: new[] { "ProviderName", "MetricName", "MetricType" });

            migrationBuilder.CreateIndex(
                name: "IX_ProviderMetricValues_MetricName_MetricType",
                table: "ProviderMetricValues",
                columns: new[] { "MetricName", "MetricType" });

            migrationBuilder.CreateIndex(
                name: "IX_UserProviderMetricValues_ProviderName_MetricName_MetricType",
                table: "UserProviderMetricValues",
                columns: new[] { "ProviderName", "MetricName", "MetricType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "CommitmentPeriodUserGoals");

            migrationBuilder.DropTable(
                name: "CommitmentUsers");

            migrationBuilder.DropTable(
                name: "Deposits");

            migrationBuilder.DropTable(
                name: "UserProviderMetricValues");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "CommitmentPeriodUsers");

            migrationBuilder.DropTable(
                name: "Goals");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "CommitmentPeriods");

            migrationBuilder.DropTable(
                name: "ProviderMetricValues");

            migrationBuilder.DropTable(
                name: "Commitments");

            migrationBuilder.DropTable(
                name: "MetricValues");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropTable(
                name: "Metrics");
        }
    }
}
