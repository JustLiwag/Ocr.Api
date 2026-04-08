using Ocr.Api.Models;
using Ocr.Api.Models.Records;
using System.Text.RegularExpressions;

namespace Ocr.Api.Services.Ocr
{
    public class DocTrTokenNormalizationService : IDocTrTokenNormalizationService
    {
        private static readonly Regex tokenRegex = new Regex(
            @"[A-Za-z0-9]+(?:[-/][A-Za-z0-9]+)*|[^\w\s]",
            RegexOptions.Compiled
        );

        public List<DocTrNormalizedToken> NormalizeWords(IEnumerable<DocTrWordResult> words)
        {
            var result = new List<DocTrNormalizedToken>();

            foreach (var word in words)
            {
                var rawText = word.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(rawText))
                    continue;

                var matches = tokenRegex.Matches(rawText);
                if (matches.Count == 0)
                {
                    result.Add(new DocTrNormalizedToken
                    {
                        RawText = rawText,
                        Text = rawText,
                        TokenType = "Mixed",
                        Confidence = word.Confidence,
                        XMin = word.XMin,
                        YMin = word.YMin,
                        XMax = word.XMax,
                        YMax = word.YMax
                    });

                    continue;
                }

                int totalLength = matches.Cast<Match>().Sum(m => m.Length);
                if (totalLength <= 0)
                    totalLength = rawText.Length > 0 ? rawText.Length : 1;

                float currentX = word.XMin;
                float totalWidth = word.XMax - word.XMin;

                foreach (Match match in matches)
                {
                    string tokenText = match.Value;
                    float portion = (float)match.Length / totalLength;
                    float tokenWidth = totalWidth * portion;

                    float tokenXMin = currentX;
                    float tokenXMax = currentX + tokenWidth;

                    result.Add(new DocTrNormalizedToken
                    {
                        RawText = rawText,
                        Text = tokenText,
                        TokenType = GetTokenType(tokenText),
                        Confidence = word.Confidence,
                        XMin = tokenXMin,
                        YMin = word.YMin,
                        XMax = tokenXMax,
                        YMax = word.YMax
                    });

                    currentX = tokenXMax;
                }
            }

            return result;
        }

        private static string GetTokenType(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return "Unknown";

            if (Regex.IsMatch(token, @"^[^\w\s]+$"))
                return "Punctuation";

            if (Regex.IsMatch(token, @"^[A-Za-z0-9]+(?:[-/][A-Za-z0-9]+)*$"))
            {
                if (token.Contains('-') || token.Contains('/'))
                    return "Identifier";

                return "Word";
            }

            return "Mixed";
        }
    }
}