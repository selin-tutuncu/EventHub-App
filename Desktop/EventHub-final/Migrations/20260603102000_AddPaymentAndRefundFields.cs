using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventHub.Migrations
{
    public partial class AddPaymentAndRefundFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "EventRegistrations",
                type: "decimal(10,2)",
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
        }
    }
}
