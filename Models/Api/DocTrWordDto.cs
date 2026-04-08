namespace Ocr.Api.Models.Api
{
    public class DocTrWordDto
    {
        public int WordOrder { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string? CorrectedText { get; set; }
        public string FinalText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float XMin { get; set; }
        public float YMin { get; set; }
        public float XMax { get; set; }
        public float YMax { get; set; }
    }
}