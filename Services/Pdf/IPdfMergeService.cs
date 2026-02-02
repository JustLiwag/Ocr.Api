namespace Ocr.Api.Services.Pdf
{
    public interface IPdfMergeService
    {
        Task<string> MergeAsync(IEnumerable<string> pdfPaths);
    }

}
