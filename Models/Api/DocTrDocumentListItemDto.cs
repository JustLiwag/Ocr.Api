namespace Ocr.Api.Models.Api
{
    public class DocTrDocumentListItemDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Engine { get; set; } = "docTR";
        public DateTime CreatedAt { get; set; }
        public float? OcrConfidence { get; set; }
        public string? Quality { get; set; }
        public string? ReviewStatus { get; set; }
        public int ReviewedPages { get; set; }
        public int CorrectedWords { get; set; }
    }
}