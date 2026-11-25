using SportsCourtBooking.Application.DTOs;
using SportsCourtBooking.Application.Interfaces;
using SportsCourtBooking.Desktop.Core;
using SportsCourtBooking.Domain.Enums;

namespace SportsCourtBooking.Desktop;

public sealed class LoginForm : Form
{
    private readonly AppServices _app;
    private readonly TextBox _email;
    private readonly TextBox _password;
    private readonly Label _error;
    private readonly Button _submit;

    public LoginForm(AppServices app, string? notice = null)
    {
        _app = app;
        Text = "Sân Nhà – Đăng nhập quản trị";
        AutoScaleMode = AutoScaleMode.None;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Theme.Surface;
        Font = Theme.Body;
        ClientSize = new Size(Ui.S(760), Ui.S(440));

        var brand = new Panel { Dock = DockStyle.Left, Width = Ui.S(300), BackColor = Theme.Sidebar, Padding = Ui.Pad(28) };
        brand.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "Duyệt cơ sở, quản lý người dùng,\nđơn đặt và báo cáo.\n\nDành cho quản trị viên.",
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = Theme.SidebarText,
            Padding = Ui.Pad(0, 12, 0, 0)
        });
        brand.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Height = Ui.S(130),
            Text = "SÂN NHÀ.",
            Font = new Font("Segoe UI", 24f, FontStyle.Bold),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.BottomLeft
        });

        var form = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = Ui.Pad(44, 48, 40, 20)
        };
        var title = Ui.Label("Đăng nhập", Theme.Title);
        title.Margin = Ui.Pad(0, 0, 0, 18);
        form.Controls.Add(title);
        _email = AddInput(form, "Email", password: false);
        _password = AddInput(form, "Mật khẩu", password: true);
        _submit = Ui.Button("Đăng nhập", async (_, _) => await SubmitAsync(), primary: true);
        _submit.Margin = Ui.Pad(0, 6, 0, 10);
        form.Controls.Add(_submit);
        _error = Ui.Label(notice ?? string.Empty, Theme.Body, Theme.Danger);
        _error.MaximumSize = new Size(Ui.S(370), 0);
        form.Controls.Add(_error);

        Controls.Add(form);
        Controls.Add(brand);
        AcceptButton = _submit;
    }

    private static TextBox AddInput(FlowLayoutPanel form, string label, bool password)
    {
        var box = new TextBox
        {
            Width = Ui.S(360),
            Font = new Font("Segoe UI", 11f),
            UseSystemPasswordChar = password,
            Margin = Ui.Pad(0, 2, 0, 14),
            AccessibleName = label
        };
        form.Controls.Add(Ui.Label(label, Theme.Body, Theme.Muted));
        form.Controls.Add(box);
        return box;
    }

    private async Task SubmitAsync()
    {
        var request = new LoginRequest { Email = _email.Text.Trim(), Password = _password.Text };
        if (Validation.Error(request) is { } error)
        {
            _error.Text = error;
            return;
        }

        _submit.Enabled = false;
        UseWaitCursor = true;
        _error.Text = string.Empty;
        try
        {
            var result = await _app.CallAsync((IAuthService service) => service.LoginAsync(request));
            if (!result.Success || result.User is null)
            {
                _error.Text = result.Message;
                _password.Clear();
                _password.Focus();
                return;
            }

            if (result.User.Role != UserRole.Admin)
            {
                _error.Text = "Ứng dụng này chỉ dành cho quản trị viên. Các tài khoản khác vui lòng dùng trang web.";
                _password.Clear();
                return;
            }

            _app.SignIn(result.User);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception exception)
        {
            _error.Text = ErrorText.For(exception);
        }
        finally
        {
            UseWaitCursor = false;
            _submit.Enabled = true;
        }
    }
}
