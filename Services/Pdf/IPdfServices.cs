namespace Ocr.Api.Services.Pdf
{
    public interface IPdfServices
    {
        // check if the PDF has text layer
        bool HasText(string pdfPath);
        // render PDF to images
        Task<string> MergeAsync( IEnumerable<string> pdfPaths, string baseDir, string outputFileName);
    }
}
