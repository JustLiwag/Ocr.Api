using System;
using System.Collections.Generic;

namespace Ocr.Api.Models.Api
{
    public class DocTrDocumentResponseDto
    {
        public string DocumentId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string Engine { get; set; } = "docTR";
        public int DeclaredPageCount { get; set; }
        public int Pages { get; set; }
        public int ReviewedPages { get; set; }
        public int NeedsRecheckPages { get; set; }
        public int NotReviewedPages { get; set; }
        public int WordCount { get; set; }
        public int CorrectedWords { get; set; }
        public float OcrConfidence { get; set; }
        public string Quality { get; set; } = "Poor";
        public string ReviewStatus { get; set; } = "NotReviewed";
        public DateTime CreatedAt { get; set; }
        public List<DocTrPageSummaryDto> PageSummaries { get; set; } = new();
    }
}