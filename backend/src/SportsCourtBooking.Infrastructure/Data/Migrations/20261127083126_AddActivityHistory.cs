using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SportsCourtBooking.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ActorId = table.Column<int>(type: "integer", nullable: false),
                    ActorRole = table.Column<int>(type: "integer", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Target = table.Column<string>(type: "text", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_ActorRole_OccurredAtUtc_Id",
                table: "ActivityRecords",
                columns: new[] { "ActorRole", "OccurredAtUtc", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityRecords_OccurredAtUtc_Id",
                table: "ActivityRecords",
                columns: new[] { "OccurredAtUtc", "Id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityRecords");
        }
    }
}
