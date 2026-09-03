using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediSphere.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorProfileImageData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProfileImageContentType",
                table: "Doctors",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "ProfileImageData",
                table: "Doctors",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "ProfileImageContentType", "ProfileImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ProfileImageContentType", "ProfileImageData" },
                values: new object[] { null, null });

            migrationBuilder.UpdateData(
                table: "Doctors",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ProfileImageContentType", "ProfileImageData" },
                values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfileImageContentType",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "ProfileImageData",
                table: "Doctors");
        }
    }
}
