namespace Ocr.Api.Models.Api
{
    public class DocTrCorrectionSuggestionDto
    {
        public string RawText { get; set; } = string.Empty;
        public string SuggestedText { get; set; } = string.Empty;
        public int Occurrences { get; set; }
    }
}