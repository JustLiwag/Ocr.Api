namespace Ocr.Api.Services.Pdf
{
    public interface IPdfTextDetector
    {
        bool HasText(string pdfPath);
    }
}
