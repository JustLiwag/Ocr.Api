namespace Ocr.Api.Services.Pdf
{
    public interface IPdfMergeService
    {
        Task<string> MergeAsync(
            IEnumerable<string> pdfPaths,
            string baseDir,
            string outputFileName);

        Task<string> MergeChunkAsync(
            IEnumerable<string> pdfPaths,
            string outputPdfPath);

        Task<string> MergeInChunksAsync(
            IEnumerable<string> pdfPaths,
            string baseDir,
            string outputFileName,
            int chunkSize = 25);
    }
}