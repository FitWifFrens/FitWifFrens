using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace FitWifFrens.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddingWorkoutMetric : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Metrics",
                column: "Name",
                value: "Workout");

            migrationBuilder.InsertData(
                table: "MetricValues",
                columns: new[] { "MetricName", "Type" },
                values: new object[,]
                {
                    { "Workout", "Count" },
                    { "Workout", "Minutes" }
                });

            migrationBuilder.InsertData(
                table: "ProviderMetricValues",
                columns: new[] { "MetricName", "MetricType", "ProviderName" },
                values: new object[,]
                {
                    { "Workout", "Count", "Strava" },
                    { "Workout", "Minutes", "Strava" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ProviderMetricValues",
                keyColumns: new[] { "MetricName", "MetricType", "ProviderName" },
                keyValues: new object[] { "Workout", "Count", "Strava" });

            migrationBuilder.DeleteData(
                table: "ProviderMetricValues",
                keyColumns: new[] { "MetricName", "MetricType", "ProviderName" },
                keyValues: new object[] { "Workout", "Minutes", "Strava" });

            migrationBuilder.DeleteData(
                table: "MetricValues",
                keyColumns: new[] { "MetricName", "Type" },
                keyValues: new object[] { "Workout", "Count" });

            migrationBuilder.DeleteData(
                table: "MetricValues",
                keyColumns: new[] { "MetricName", "Type" },
                keyValues: new object[] { "Workout", "Minutes" });

            migrationBuilder.DeleteData(
                table: "Metrics",
                keyColumn: "Name",
                keyValue: "Workout");
        }
    }
}
