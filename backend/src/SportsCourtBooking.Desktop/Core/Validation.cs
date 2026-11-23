using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace SportsCourtBooking.Desktop.Core;

/// <summary>Kiểm tra DataAnnotations trên các request, giống bước ModelState của ASP.NET MVC.</summary>
public static class Validation
{
    public static string? Error(object request)
    {
        // Các cận [Range] viết dạng "0.01" nên phải đọc theo văn hóa bất biến, không theo vi-VN.
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        try
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
            var messages = results
                .Select(result => result.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct();
            var text = string.Join(Environment.NewLine, messages);
            return text.Length == 0 ? null : text;
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }
}
