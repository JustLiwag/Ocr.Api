namespace Ocr.Api.Models
{
    public class DocTrPageReviewRequest
    {
        public string ReviewStatus { get; set; } = "Reviewed";
        public string? ReviewedBy { get; set; }
    }
}