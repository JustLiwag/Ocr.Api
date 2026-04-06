namespace Ocr.Api.Models
{
    public class OcrResult
    {
        public string PdfPath { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string? TextPath { get; set; }
        public string? TsvPath { get; set; }
    }
}