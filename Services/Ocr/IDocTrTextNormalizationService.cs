namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrTextNormalizationService
    {
        string NormalizeForSuggestion(string? text);
    }
}