namespace Ocr.Api.Models.Records
{
    public class DocTrWordRecord
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int WordOrder { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string? CorrectedText { get; set; }
        public string FinalText => string.IsNullOrWhiteSpace(CorrectedText) ? RawText : CorrectedText;
        public float Confidence { get; set; }
        public float XMin { get; set; }
        public float YMin { get; set; }
        public float XMax { get; set; }
        public float YMax { get; set; }
        public string TokenType { get; set; } = "Word";
        public string NormalizedText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}