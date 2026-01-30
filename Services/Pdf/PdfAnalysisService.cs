namespace Ocr.Api.Services.Pdf
{
    public class PdfPageAnalysis
    {
        public int PageNumber { get; set; }
        public bool HasText { get; set; }
        public int TextLength { get; set; } // number of characters detected
    }

    public class PdfAnalysisResult
    {
        public int TotalPages { get; set; }
        public List<PdfPageAnalysis> Pages { get; set; } = new();
        public bool IsSearchable => Pages.All(p => p.HasText);
    }

}
