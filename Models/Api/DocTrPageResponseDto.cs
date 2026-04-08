using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrPageResponseDto
    {
        public DocTrPageDto? Page { get; set; }
        public List<DocTrWordDto> Words { get; set; } = new();
    }
}