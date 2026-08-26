using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberSelfService : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicationUserId",
                table: "Borrowers",
                type: "TEXT",
                maxLength: 450,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Borrowers_ApplicationUserId",
                table: "Borrowers",
                column: "ApplicationUserId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Borrowers_AspNetUsers_ApplicationUserId",
                table: "Borrowers",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Borrowers_AspNetUsers_ApplicationUserId",
                table: "Borrowers");

            migrationBuilder.DropIndex(
                name: "IX_Borrowers_ApplicationUserId",
                table: "Borrowers");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Borrowers");
        }
    }
}
