using SportsCourtBooking.Infrastructure.Data;
using SportsCourtBooking.Infrastructure.Security;

namespace SportsCourtBooking.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_ThenVerify_Succeeds()
    {
        var hash = _hasher.Hash("MatKhau@123");

        Assert.True(_hasher.Verify("MatKhau@123", hash));
    }

    [Fact]
    public void Verify_WrongPassword_Fails()
    {
        var hash = _hasher.Hash("MatKhau@123");

        Assert.False(_hasher.Verify("matkhau@123", hash));
    }

    [Fact]
    public void Hash_DoesNotContainPlainPassword_AndUsesRandomSalt()
    {
        var first = _hasher.Hash("MatKhau@123");
        var second = _hasher.Hash("MatKhau@123");

        Assert.DoesNotContain("MatKhau@123", first);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Admin@123")]                       // mật khẩu chữ thường (định dạng cũ) không còn được chấp nhận
    [InlineData("PBKDF2$abc$xx$yy")]
    [InlineData("PBKDF2$600000$not-base64$not-base64")]
    [InlineData("OTHER$600000$AAAA$AAAA")]
    public void Verify_MalformedOrLegacyHash_ReturnsFalse(string storedHash)
    {
        Assert.False(_hasher.Verify("Admin@123", storedHash));
    }

    [Fact]
    public void SeedAccounts_HashesMatchDocumentedPasswords()
    {
        Assert.True(_hasher.Verify("Admin@123", SeedCredentials.AdminPasswordHash));
        Assert.True(_hasher.Verify("Customer@123", SeedCredentials.CustomerPasswordHash));
        Assert.False(_hasher.Verify("Customer@123", SeedCredentials.AdminPasswordHash));
    }
}
