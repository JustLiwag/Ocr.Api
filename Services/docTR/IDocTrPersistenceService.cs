using Ocr.Api.Models;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrPersistenceService
    {
        Task SaveDocumentAsync(DocTrDocumentRecord documentRecord);
        Task<DocTrDocumentRecord?> GetDocumentAsync(string documentId);
        Task SavePageAsync(DocTrPageRecord pageRecord);
        Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords);
        Task<DocTrPageRecord?> GetPageAsync(string documentId, int pageNumber);
        Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber);
        Task SaveCorrectionsAsync(string documentId, int pageNumber, IEnumerable<DocTrWordCorrectionRequest> corrections);
        Task<List<int>> GetPageNumbersAsync(string documentId);
        
    }
}