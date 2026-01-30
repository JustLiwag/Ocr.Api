using UglyToad.PdfPig;
using System.Linq;

namespace Ocr.Api.Services.Pdf
{
    public class PdfPigTextDetector : IPdfTextDetector
    {
        public bool HasText(string pdfPath)
        {
            using var document = PdfDocument.Open(pdfPath);

            return document.GetPages()
                .Any(p => p.Letters.Count > 20);
        }
    }
}
