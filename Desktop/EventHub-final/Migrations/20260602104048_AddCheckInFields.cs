using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Migrations
{
    public partial class AddCheckInFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckInCode",
                table: "EventRegistrations",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "EventRegistrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "c3d4e5f6-a7b8-9012-cdef-012345678902",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "a322c852-145d-44a3-9452-51043c356304", "AQAAAAEAACcQAAAAENRJndmEi1Jz7spH1pwHa0pBoPCM0rbVJgoY8x26QmDiat1pSYPxYgMG1QAzrZAWmA==", "6767b877-cd04-44c8-9fca-54721a56ac3e" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckInCode",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "EventRegistrations");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "c3d4e5f6-a7b8-9012-cdef-012345678902",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "88380447-360b-4d01-9520-525634c9aadf", "AQAAAAEAACcQAAAAEBnlpJYVNojnvZmpqeo9QdrXSIDE+r1dSDzjAytSB6l4HDWURKG8oE7R8SEOgkLuhg==", "289c15f9-5274-4cad-b458-2a2ffc18c393" });
        }
    }
}
