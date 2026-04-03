using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DevHelp.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreferredProfessorForTicket : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreferredProfessorId",
                table: "Tickets",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PreferredProfessorId",
                table: "Tickets",
                column: "PreferredProfessorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tickets_AspNetUsers_PreferredProfessorId",
                table: "Tickets",
                column: "PreferredProfessorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_AspNetUsers_PreferredProfessorId",
                table: "Tickets");

            migrationBuilder.DropIndex(
                name: "IX_Tickets_PreferredProfessorId",
                table: "Tickets");

            migrationBuilder.DropColumn(
                name: "PreferredProfessorId",
                table: "Tickets");
        }
    }
}
