namespace Ocr.Api.Models
{
    public class DocTrDocumentRecord
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int PageCount { get; set; }
        public string Engine { get; set; } = "docTR";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}   