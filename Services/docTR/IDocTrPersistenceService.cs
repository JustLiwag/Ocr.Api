using Ocr.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrPersistenceService
    {
        Task SavePageAsync(DocTrPageRecord pageRecord);
        Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords);

        Task<DocTrPageRecord?> GetPageAsync(string documentId, int pageNumber);
        Task<List<DocTrWordRecord>> GetWordsAsync(string documentId, int pageNumber);
        Task SaveCorrectionsAsync(string documentId, int pageNumber, IEnumerable<DocTrWordCorrectionRequest> corrections);
    }
}