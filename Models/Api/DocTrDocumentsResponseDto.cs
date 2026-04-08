using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrDocumentsResponseDto
    {
        public int Count { get; set; }
        public List<DocTrDocumentListItemDto> Documents { get; set; } = new();
    }
}