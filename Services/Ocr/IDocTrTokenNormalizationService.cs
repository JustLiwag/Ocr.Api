using Ocr.Api.Models;
using Ocr.Api.Models.Records;
using System.Collections.Generic;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrTokenNormalizationService
    {
        List<DocTrNormalizedToken> NormalizeWords(IEnumerable<DocTrWordResult> words);
    }
}