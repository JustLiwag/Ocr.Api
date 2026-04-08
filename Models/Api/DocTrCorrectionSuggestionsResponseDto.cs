using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrCorrectionSuggestionsResponseDto
    {
        public string RawText { get; set; } = string.Empty;
        public int Count { get; set; }
        public List<DocTrCorrectionSuggestionDto> Suggestions { get; set; } = new();
    }
}