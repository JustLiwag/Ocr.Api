namespace Ocr.Api.Models
{
    public class OcrResult
    {
        public string PdfPath { get; set; }
        public float Confidence { get; set; }
        public string TextPath { get; set; }   // extracted text
        public string TsvPath { get; set; }
    }
}
