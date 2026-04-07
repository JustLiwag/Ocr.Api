using Ocr.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Pdf
{
    public interface ISearchablePdfBuilderService
    {
        Task<string> BuildPagePdfAsync(
            string imagePath,
            IEnumerable<DocTrWordResult> words,
            string outputPdfPath);
    }
}