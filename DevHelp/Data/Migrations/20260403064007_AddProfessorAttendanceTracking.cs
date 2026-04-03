using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHelp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfessorAttendanceTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssignedProfessorId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceFinishedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceStartedAtUtc",
                table: "Tickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssignedProfessorId",
                table: "Tickets",
                column: "AssignedProfessorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_AssignedProfessorId",
                table: "Tickets",
                column: "AssignedProfessorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_AssignedProfessorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_AssignedProfessorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "AssignedProfessorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ServiceFinishedAtUtc",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "ServiceStartedAtUtc",
                table: "Tickets");
        }
    }
}
