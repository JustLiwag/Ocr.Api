namespace Ocr.Api.Models.Api
{
    public class DocTrPageSummaryDto
    {
        public int PageNumber { get; set; }
        public string Engine { get; set; } = "docTR";
        public int WordCount { get; set; }
        public int CorrectedWords { get; set; }
        public float OcrConfidence { get; set; }
        public string Quality { get; set; } = "Poor";
        public string ReviewStatus { get; set; } = "NotReviewed";
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string SourceImagePath { get; set; } = string.Empty;
    }
}