namespace Ocr.Api.Models.Records
{
    public class DocTrWordCorrectionRequest
    {
        public int WordOrder { get; set; }
        public string CorrectedText { get; set; } = string.Empty;
        public string? CorrectedBy { get; set; }
    }
}