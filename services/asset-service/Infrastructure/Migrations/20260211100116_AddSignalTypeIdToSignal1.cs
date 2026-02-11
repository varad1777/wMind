using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalTypeIdToSignal1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SignalTypeId",
                table: "Signals",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Signals_SignalTypeId",
                table: "Signals",
                column: "SignalTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Signals_SignalTypes_SignalTypeId",
                table: "Signals",
                column: "SignalTypeId",
                principalTable: "SignalTypes",
                principalColumn: "SignalTypeID",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Signals_SignalTypes_SignalTypeId",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Signals_SignalTypeId",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "SignalTypeId",
                table: "Signals");
        }
    }
}
