using Microsoft.EntityFrameworkCore;

namespace SportsCourtBooking.Infrastructure.Data;

public static class DatabaseSequenceExtensions
{
    // Các bảng có dữ liệu mẫu (HasData) với Id gán cứng. PostgreSQL không tự đẩy bộ đếm Id lên khi
    // chèn Id thủ công, nên phải đặt lại bộ đếm = Id lớn nhất hiện có.
    private static readonly string[] SeededTables = { "Users", "CourtTypes", "Courts" };

    public static void SyncIdentitySequences(this AppDbContext db)
    {
        if (!db.Database.IsNpgsql())
            return;

        foreach (var table in SeededTables)
        {
            db.Database.ExecuteSqlRaw(
                $"SELECT setval(pg_get_serial_sequence('\"{table}\"', 'Id'), " +
                $"COALESCE((SELECT MAX(\"Id\") FROM \"{table}\"), 1), " +
                $"(SELECT MAX(\"Id\") FROM \"{table}\") IS NOT NULL)");
        }
    }
}
