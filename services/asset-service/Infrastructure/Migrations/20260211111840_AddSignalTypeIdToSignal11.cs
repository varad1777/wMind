using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalTypeIdToSignal11 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_MappingId",
                table: "Alerts");

            migrationBuilder.AlterColumn<double>(
                name: "MinObservedValue",
                table: "Alerts",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "MaxObservedValue",
                table: "Alerts",
                type: "float",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MappingId",
                table: "Alerts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "SignalId",
                table: "Alerts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Alert_Signal_Active",
                table: "Alerts",
                columns: new[] { "SignalId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AlertStartUtc",
                table: "Alerts",
                column: "AlertStartUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_MappingId_IsActive",
                table: "Alerts",
                columns: new[] { "MappingId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alert_Signal_Active",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_AlertStartUtc",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_MappingId_IsActive",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "SignalId",
                table: "Alerts");

            migrationBuilder.AlterColumn<double>(
                name: "MinObservedValue",
                table: "Alerts",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<double>(
                name: "MaxObservedValue",
                table: "Alerts",
                type: "float",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float");

            migrationBuilder.AlterColumn<Guid>(
                name: "MappingId",
                table: "Alerts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_MappingId",
                table: "Alerts",
                column: "MappingId");
        }
    }
}
