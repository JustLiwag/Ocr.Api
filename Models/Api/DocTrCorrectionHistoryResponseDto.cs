using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrCorrectionHistoryResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int WordOrder { get; set; }
        public List<DocTrCorrectionHistoryItemDto> History { get; set; } = new();
    }
}