using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameTokenHashToOtpHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TokenHash",
                table: "PasswordResetTokens",
                newName: "OtpHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OtpHash",
                table: "PasswordResetTokens",
                newName: "TokenHash");
        }
    }
}
