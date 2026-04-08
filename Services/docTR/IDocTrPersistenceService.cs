using Ocr.Api.Models.Api;
using Ocr.Api.Models.Records;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrPersistenceService
    {
        Task SaveDocumentAsync(DocTrDocumentRecord documentRecord);
        Task<DocTrDocumentRecord?> GetDocumentAsync(string documentId);
        Task<List<DocTrDocumentRecord>> GetDocumentsAsync(string? reviewStatus = null);
        Task SavePageAsync(DocTrPageRecord pageRecord);
        Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords);
        Task<DocTrPageRecord?> GetPageAsync(string documentId, int pageNumber);
        Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber);
        Task SaveCorrectionsAsync(string documentId, int pageNumber, IEnumerable<DocTrWordCorrectionRequest> corrections);
        Task<List<int>> GetPageNumbersAsync(string documentId);
        Task<List<DocTrWordCorrectionHistoryRecord>> GetCorrectionHistoryAsync(string documentId, int pageNumber, int wordOrder);
        Task UpdatePageReviewAsync(string documentId, int pageNumber, string reviewStatus, string? reviewedBy);
        Task<List<DocTrCorrectionSuggestionDto>> GetCorrectionSuggestionsAsync(string rawText, int top = 5);
        Task<Dictionary<string, List<DocTrCorrectionSuggestionDto>>> GetCorrectionSuggestionsBatchAsync(IEnumerable<string> rawTexts, int top = 5);

    }
}