using System.Collections.Generic;
using System.Linq;
using UglyToad.PdfPig;

namespace Ocr.Api.Services.Pdf
{
    public interface IPdfAnalysisService
    {
        PdfAnalysisResult Analyze(string pdfPath);
    }

    public class PdfAnalysisService : IPdfAnalysisService
    {
        public PdfAnalysisResult Analyze(string pdfPath)
        {
            var result = new PdfAnalysisResult();

            using var pdf = PdfDocument.Open(pdfPath);
            result.TotalPages = pdf.NumberOfPages;

            for (int i = 0; i < pdf.NumberOfPages; i++)
            {
                var page = pdf.GetPage(i + 1);
                var text = page.Text;
                result.Pages.Add(new PdfPageAnalysis
                {
                    PageNumber = i + 1,
                    HasText = !string.IsNullOrWhiteSpace(text),
                    TextLength = text.Length
                });
            }

            return result;
        }
    }

    public class PdfPageAnalysis
    {
        public int PageNumber { get; set; }
        public bool HasText { get; set; }
        public int TextLength { get; set; }
    }

    public class PdfAnalysisResult
    {
        public int TotalPages { get; set; }
        public List<PdfPageAnalysis> Pages { get; set; } = new List<PdfPageAnalysis>();
        public bool IsSearchable => Pages.All(p => p.HasText);
    }
}
