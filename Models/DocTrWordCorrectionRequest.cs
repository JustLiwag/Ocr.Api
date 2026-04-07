namespace Ocr.Api.Models
{
    public class DocTrWordCorrectionRequest
    {
        public int WordOrder { get; set; }
        public string CorrectedText { get; set; } = string.Empty;
    }
}