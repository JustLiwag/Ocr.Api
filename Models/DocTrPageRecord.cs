namespace Ocr.Api.Models
{
    public class DocTrPageRecord
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public string Engine { get; set; } = "docTR";
        public string SourceImagePath { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}