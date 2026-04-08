namespace Ocr.Api.Models.Records
{
    public class DocTrWordCorrectionHistoryRecord
    {
        public long HistoryId { get; set; }
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int WordOrder { get; set; }
        public string? OldText { get; set; }
        public string? NewText { get; set; }
        public DateTime CorrectedAt { get; set; }
        public string? CorrectedBy { get; set; }
    }
}