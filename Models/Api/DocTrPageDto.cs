namespace Ocr.Api.Models.Api
{
    public class DocTrPageDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Engine { get; set; } = "docTR";
        public string SourceImagePath { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float OcrConfidence { get; set; }
        public string Quality { get; set; } = "Poor";
        public string ReviewStatus { get; set; } = "NotReviewed";
        public string? ReviewedBy { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}