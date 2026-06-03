using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Migrations
{
    public partial class AddPaymentFieldsAuto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "EventRegistrations",
                type: "decimal(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymentCompletedAt",
                table: "EventRegistrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentFullName",
                table: "EventRegistrations",
                type: "TEXT",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "EventRegistrations",
                type: "TEXT",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundNote",
                table: "EventRegistrations",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundRequestedAt",
                table: "EventRegistrations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "c3d4e5f6-a7b8-9012-cdef-012345678902",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "2fd3f9bb-352c-4dba-b084-85b864eb79aa", "AQAAAAEAACcQAAAAEN07RMolZmjMjqY9qthEk1XaOzOE4WBaNrHJkDY4RtnxBrJUboPM39nV4pm6HMEcIg==", "911a3323-10b0-4713-a37a-9305e741a11d" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "PaymentCompletedAt",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "PaymentFullName",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "RefundNote",
                table: "EventRegistrations");

            migrationBuilder.DropColumn(
                name: "RefundRequestedAt",
                table: "EventRegistrations");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "c3d4e5f6-a7b8-9012-cdef-012345678902",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "f18040ad-2429-4789-90be-6c66e1ba653b", "AQAAAAEAACcQAAAAEPz2mpSi9DRdIr1K15PznmpHxNI8ayhpndM62TnTJ1GTaOVrLySRiOSGWOkZ3/Ud2w==", "c68418cc-9a61-4cb1-8dc4-f71c42f70b32" });
        }
    }
}
