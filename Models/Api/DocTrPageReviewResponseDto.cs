namespace Ocr.Api.Models.Api
{
    public class DocTrPageReviewResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public DocTrPageDto? Page { get; set; }
    }
}