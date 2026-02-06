using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProtocolSettingsJson",
                table: "DeviceConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "Endian",
                table: "DeviceConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "DeviceConfigurations",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "DeviceConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SlaveId",
                table: "DeviceConfigurations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Endian",
                table: "DeviceConfigurations");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "DeviceConfigurations");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "DeviceConfigurations");

            migrationBuilder.DropColumn(
                name: "SlaveId",
                table: "DeviceConfigurations");

            migrationBuilder.AddColumn<string>(
                name: "ProtocolSettingsJson",
                table: "DeviceConfigurations",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
