using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrCorrectionsResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public int PageNumber { get; set; }
        public int CorrectedWords { get; set; }
        public List<DocTrWordDto> Words { get; set; } = new();
    }
}