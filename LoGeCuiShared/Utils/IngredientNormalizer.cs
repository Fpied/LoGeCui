using System.Globalization;
using System.Text;

namespace LoGeCuiShared.Utils
{
    public static class IngredientNormalizer
    {
        public static string Normalize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";

            s = s.Trim().ToLowerInvariant();

            // 🔹 Enlever les accents
            s = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            s = sb.ToString().Normalize(NormalizationForm.FormC);

            // 🔹 Remplacer la ponctuation par des espaces
            var sb2 = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (char.IsLetterOrDigit(ch) || ch == ' ')
                    sb2.Append(ch);
                else
                    sb2.Append(' ');
            }
            s = sb2.ToString();

            // 🔹 Nettoyer espaces multiples
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            s = s.Trim();

            // 🔹 Pluriels simples
            if (s.EndsWith("s") && s.Length > 3)
                s = s[..^1];

            // 🔹 Pluriels irréguliers courants
            if (s == "oeufs") return "oeuf";

            return s;
        }
    }
}
