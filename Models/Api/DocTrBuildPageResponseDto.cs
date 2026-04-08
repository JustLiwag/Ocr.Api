namespace Ocr.Api.Models.Api
{
    public class DocTrBuildPageResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string File { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public int RenderDpi { get; set; }
        public string Engine { get; set; } = "docTR";
        public float EngineConfidence { get; set; }
        public float OcrConfidence { get; set; }
        public string Quality { get; set; } = "Poor";
        public string FullText { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public string SourceImagePath { get; set; } = string.Empty;
        public string OutputPdf { get; set; } = string.Empty;
        public string TimeElapsed { get; set; } = "00:00:00";
    }
}