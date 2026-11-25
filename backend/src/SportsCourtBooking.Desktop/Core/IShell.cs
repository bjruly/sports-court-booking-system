namespace SportsCourtBooking.Desktop.Core;

/// <summary>Các thao tác mà một màn hình có thể yêu cầu cửa sổ chính thực hiện.</summary>
public interface IShell
{
    void ShowMessage(string message, bool success);

    void Navigate(string pageKey);

    void ForceSignOut(string reason);
}
