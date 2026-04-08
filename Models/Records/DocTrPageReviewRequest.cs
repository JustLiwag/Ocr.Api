namespace Ocr.Api.Models.Records
{
    public class DocTrPageReviewRequest
    {
        public string ReviewStatus { get; set; } = "Reviewed";
        public string? ReviewedBy { get; set; }
    }
}