using System.Security.Cryptography;
using SportsCourtBooking.Application.Interfaces;

namespace SportsCourtBooking.Infrastructure.Security;

/// <summary>
/// Băm mật khẩu bằng PBKDF2-HMAC-SHA256 kèm salt ngẫu nhiên (chỉ dùng thư viện có sẵn của .NET).
/// Định dạng lưu trong database: PBKDF2$số_vòng_lặp$salt_base64$hash_base64
/// Số vòng lặp được lưu cùng hash nên sau này có thể tăng mà vẫn đăng nhập được với hash cũ.
/// </summary>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "PBKDF2";
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 600_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, KeySize);

        return string.Join("$", Prefix, Iterations.ToString(), Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public bool Verify(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 4 || parts[0] != Prefix || !int.TryParse(parts[1], out var iterations) || iterations <= 0)
        {
            return false;
        }

        byte[] salt;
        byte[] expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expected.Length == 0)
        {
            return false;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
