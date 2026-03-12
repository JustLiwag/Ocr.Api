using System.Text.RegularExpressions;

namespace Ocr.Api.Services.Helpers
{
    /*
        =========================================================
        OcrTextNormalizer
        ---------------------------------------------------------
        Purpose:
        - Normalizes OCR text before suggestion matching
        - Reduces spacing/casing inconsistencies
        - Helps learned corrections match more reliably
        =========================================================
    */

    public static class OcrTextNormalizer
    {
        public static string normalize(string? inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return string.Empty;
            }

            string normalizedText = inputText.Trim().ToUpperInvariant();

            normalizedText = Regex.Replace(normalizedText, @"\s+", " ");

            return normalizedText;
        }

        public static string normalizeForLooseMatch(string? inputText)
        {
            if (string.IsNullOrWhiteSpace(inputText))
            {
                return string.Empty;
            }

            string normalizedText = normalize(inputText);

            normalizedText = normalizedText
                .Replace("0", "O")
                .Replace("1", "I")
                .Replace("5", "S");

            return normalizedText;
        }
    }
}
