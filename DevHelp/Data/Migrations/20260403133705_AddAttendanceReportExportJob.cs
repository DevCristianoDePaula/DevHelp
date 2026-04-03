using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHelp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceReportExportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AttendanceReportExportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequesterId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SearchTerm = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    PriorityFilter = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateToUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutputFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RelativePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceReportExportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceReportExportJobs_AspNetUsers_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReportExportJobs_RequesterId_CreatedAtUtc",
                table: "AttendanceReportExportJobs",
                columns: new[] { "RequesterId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceReportExportJobs_Status_CreatedAtUtc",
                table: "AttendanceReportExportJobs",
                columns: new[] { "Status", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AttendanceReportExportJobs");
        }
    }
}
