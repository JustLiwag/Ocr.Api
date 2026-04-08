namespace Ocr.Api.Models.Api
{
    public class DocTrRebuildPageResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int WordCount { get; set; }
        public int CorrectedWords { get; set; }
        public float OcrConfidence { get; set; }
        public string Quality { get; set; } = "Poor";
        public string OutputPdf { get; set; } = string.Empty;
        public string TimeElapsed { get; set; } = "00:00:00";
    }
}