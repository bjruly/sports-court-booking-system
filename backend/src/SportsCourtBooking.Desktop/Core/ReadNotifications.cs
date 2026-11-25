using System.Text.Json;

namespace SportsCourtBooking.Desktop.Core;

/// <summary>Ghi nhớ thông báo đã đọc theo từng tài khoản, lưu trên máy đang dùng.</summary>
public sealed class ReadNotifications
{
    private readonly string _path;
    private HashSet<string> _ids;

    public ReadNotifications(int userId)
    {
        _path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SportsCourtBooking", $"read-notifications-{userId}.json");
        try
        {
            _ids = File.Exists(_path)
                ? JsonSerializer.Deserialize<HashSet<string>>(File.ReadAllText(_path)) ?? new()
                : new();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            _ids = new();
        }
    }

    public bool IsRead(string id) => _ids.Contains(id);

    /// <summary>Chỉ giữ các thông báo còn xuất hiện để file không phình to.</summary>
    public void MarkRead(IEnumerable<string> ids)
    {
        _ids = ids.ToHashSet();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(_path, JsonSerializer.Serialize(_ids));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Không lưu được thì lần mở sau thông báo chỉ hiện lại là chưa đọc.
        }
    }
}
