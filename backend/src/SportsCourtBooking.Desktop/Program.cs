using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Infrastructure;
using WinFormsApp = System.Windows.Forms.Application;

namespace SportsCourtBooking.Desktop;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        CultureInfo.DefaultThreadCurrentCulture = Display.Culture;
        CultureInfo.DefaultThreadCurrentUICulture = Display.Culture;
        CultureInfo.CurrentCulture = Display.Culture;
        WinFormsApp.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        WinFormsApp.ThreadException += (_, e) => MessageBox.Show(
            ErrorText.For(e.Exception), "Sân Nhà", MessageBoxButtons.OK, MessageBoxIcon.Error);

        var connectionString = ConnectionSettings.Resolve(AppContext.BaseDirectory);
        if (connectionString is null)
        {
            MessageBox.Show(
                "Chưa có cấu hình kết nối database. Hãy tạo file .env ở thư mục gốc dự án (xem .env.example), "
                + "hoặc điền ConnectionStrings:DefaultConnection trong appsettings.json cạnh file chạy.",
                "Sân Nhà", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        using var provider = new ServiceCollection()
            .AddSportsCourtServices(connectionString)
            .AddLogging()
            .BuildServiceProvider();
        var app = new AppServices(provider);

        string? notice = null;
        while (true)
        {
            using (var login = new LoginForm(app, notice))
            {
                if (login.ShowDialog() != DialogResult.OK)
                {
                    return;
                }
            }

            using var main = new MainForm(app);
            WinFormsApp.Run(main);
            if (!main.SignedOut)
            {
                return;
            }

            notice = main.SignOutReason;
        }
    }
}
