using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingCommitmentTelegramPoll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "TelegramUserId",
                table: "AspNetUsers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CommitmentTelegramPollRules",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    RequireDailyResponses = table.Column<bool>(type: "boolean", nullable: false),
                    AllowsMultipleAnswers = table.Column<bool>(type: "boolean", nullable: false),
                    IsAnonymous = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentTelegramPollRules", x => x.CommitmentId);
                    table.ForeignKey(
                        name: "FK_CommitmentTelegramPollRules_Commitments_CommitmentId",
                        column: x => x.CommitmentId,
                        principalTable: "Commitments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentTelegramPollRuleOptions",
                columns: table => new
                {
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentTelegramPollRuleOptions", x => new { x.CommitmentId, x.Index });
                    table.ForeignKey(
                        name: "FK_CommitmentTelegramPollRuleOptions_CommitmentTelegramPollRul~",
                        column: x => x.CommitmentId,
                        principalTable: "CommitmentTelegramPollRules",
                        principalColumn: "CommitmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommitmentTelegramPolls",
                columns: table => new
                {
                    PollId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CommitmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: false),
                    ChatId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SentTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommitmentTelegramPolls", x => x.PollId);
                    table.ForeignKey(
                        name: "FK_CommitmentTelegramPolls_CommitmentTelegramPollRules_Commitm~",
                        column: x => x.CommitmentId,
                        principalTable: "CommitmentTelegramPollRules",
                        principalColumn: "CommitmentId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTelegramPollResponses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UpdateId = table.Column<long>(type: "bigint", nullable: false),
                    PollId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OptionIndex = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    TelegramUserId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: true),
                    AnsweredTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTelegramPollResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTelegramPollResponses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_UserTelegramPollResponses_CommitmentTelegramPolls_PollId",
                        column: x => x.PollId,
                        principalTable: "CommitmentTelegramPolls",
                        principalColumn: "PollId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TelegramUserId",
                table: "AspNetUsers",
                column: "TelegramUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CommitmentTelegramPolls_CommitmentId",
                table: "CommitmentTelegramPolls",
                column: "CommitmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTelegramPollResponses_AnsweredTime",
                table: "UserTelegramPollResponses",
                column: "AnsweredTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserTelegramPollResponses_PollId",
                table: "UserTelegramPollResponses",
                column: "PollId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTelegramPollResponses_PollId_TelegramUserId",
                table: "UserTelegramPollResponses",
                columns: new[] { "PollId", "TelegramUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTelegramPollResponses_UpdateId",
                table: "UserTelegramPollResponses",
                column: "UpdateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTelegramPollResponses_UserId",
                table: "UserTelegramPollResponses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommitmentTelegramPollRuleOptions");

            migrationBuilder.DropTable(
                name: "UserTelegramPollResponses");

            migrationBuilder.DropTable(
                name: "CommitmentTelegramPolls");

            migrationBuilder.DropTable(
                name: "CommitmentTelegramPollRules");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TelegramUserId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TelegramUserId",
                table: "AspNetUsers");
        }
    }
}
