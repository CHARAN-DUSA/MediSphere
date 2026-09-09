using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MediSphere.Infrastructure.Migrations
{
    public partial class FixAppointmentSlotUniqueIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
                table: "Appointments",
                columns: new[]
                {
                    "DoctorId",
                    "AppointmentDate",
                    "StartTime"
                },
                unique: true,
                filter: "[Status] <> 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_AppointmentDate_StartTime",
                table: "Appointments",
                columns: new[]
                {
                    "DoctorId",
                    "AppointmentDate",
                    "StartTime"
                },
                unique: true);
        }
    }
}