using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsCourtBooking.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261121130000_BackfillWalkInPayments")]
public sealed class BackfillWalkInPayments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            INSERT INTO "Payments"
                ("BookingId", "Amount", "PaymentMethod", "Status", "PaidAt", "CreatedAt")
            SELECT
                booking."Id",
                booking."TotalPrice",
                'CashAtVenue',
                2,
                booking."CreatedAt",
                booking."CreatedAt"
            FROM "Bookings" AS booking
            WHERE booking."UserId" IS NULL
              AND booking."GuestName" IS NOT NULL
              AND booking."Status" IN (2, 3, 4)
              AND NOT EXISTS (
                  SELECT 1
                  FROM "Payments" AS payment
                  WHERE payment."BookingId" = booking."Id");
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Keep the recorded cash payments; deleting them could erase legitimate payments.
    }
}
