namespace Ocr.Api.Services.Ocr
{
    public class DocTrTextNormalizationService : IDocTrTextNormalizationService
    {
        public string NormalizeForSuggestion(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? string.Empty
                : text.Trim().ToUpperInvariant();
        }
    }
}