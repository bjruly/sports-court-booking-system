namespace SportsCourtBooking.Desktop.Core;

public static class Theme
{
    public static readonly Color Sidebar = Color.FromArgb(0x0F, 0x24, 0x1C);
    public static readonly Color SidebarActive = Color.FromArgb(0x1C, 0x45, 0x35);
    public static readonly Color SidebarText = Color.FromArgb(0xD5, 0xE3, 0xDB);
    public static readonly Color SidebarMuted = Color.FromArgb(0x86, 0xA3, 0x95);

    public static readonly Color Background = Color.FromArgb(0xF3, 0xF6, 0xF4);
    public static readonly Color Surface = Color.White;
    public static readonly Color Border = Color.FromArgb(0xDD, 0xE4, 0xE0);
    public static readonly Color Text = Color.FromArgb(0x17, 0x23, 0x1D);
    public static readonly Color Muted = Color.FromArgb(0x5F, 0x6E, 0x66);

    public static readonly Color Primary = Color.FromArgb(0x15, 0x80, 0x3D);
    public static readonly Color Selection = Color.FromArgb(0xDC, 0xF1, 0xE3);

    public static readonly Color Success = Color.FromArgb(0x15, 0x80, 0x3D);
    public static readonly Color Info = Color.FromArgb(0x1D, 0x4E, 0xD8);
    public static readonly Color Warning = Color.FromArgb(0xB4, 0x53, 0x09);
    public static readonly Color Danger = Color.FromArgb(0xB9, 0x1C, 0x1C);

    // Bộ màu biểu đồ đã kiểm tra khả năng phân biệt với người mù màu.
    public static readonly Color ChartGreen = Color.FromArgb(0x15, 0x80, 0x3D);
    public static readonly Color ChartBlue = Color.FromArgb(0x25, 0x63, 0xEB);
    public static readonly Color ChartOrange = Color.FromArgb(0xEA, 0x58, 0x0C);

    public static readonly Font Body = new("Segoe UI", 9.75f);
    public static readonly Font BodyBold = new("Segoe UI", 9.75f, FontStyle.Bold);
    public static readonly Font Small = new("Segoe UI", 8.75f);
    public static readonly Font SmallBold = new("Segoe UI", 8.25f, FontStyle.Bold);
    public static readonly Font Section = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font Title = new("Segoe UI", 17f, FontStyle.Bold);
    public static readonly Font Stat = new("Segoe UI", 18f, FontStyle.Bold);
}
