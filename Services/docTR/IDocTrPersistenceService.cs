using Ocr.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrPersistenceService
    {
        Task SavePageAsync(DocTrPageRecord pageRecord);
        Task SaveWordsAsync(IEnumerable<DocTrWordRecord> wordRecords);
    }
}