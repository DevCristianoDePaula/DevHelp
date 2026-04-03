using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHelp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicTicketCallAnnouncements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketCallAnnouncements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StudentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CalledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCallAnnouncements", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketCallAnnouncements_CalledAtUtc",
                table: "TicketCallAnnouncements",
                column: "CalledAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketCallAnnouncements");
        }
    }
}
