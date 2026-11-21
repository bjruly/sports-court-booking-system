using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SportsCourtBooking.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261121100000_AddRefundRejectionReason")]
public sealed class AddRefundRejectionReason : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Refunds\" ADD COLUMN IF NOT EXISTS \"RejectionReason\" character varying(500);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("ALTER TABLE \"Refunds\" DROP COLUMN IF EXISTS \"RejectionReason\";");
    }
}
