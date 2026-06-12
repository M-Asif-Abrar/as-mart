// Services/SlugService.cs
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace AsMart.Web.Services
{
    public class SlugService : ISlugService
    {
        public string GenerateSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string normalized = input.ToLowerInvariant().Normalize(NormalizationForm.FormD);

            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

            // Replace anything that is not letter/digit with hyphen
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9]+", "-");

            // Trim hyphens
            cleaned = cleaned.Trim('-');

            return cleaned;
        }
    }
}
