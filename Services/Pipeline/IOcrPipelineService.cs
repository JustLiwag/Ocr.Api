using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Pipeline
{
    public interface IOcrPipelineService
    {
        Task<object> MergeSearchableAsync(List<IFormFile> files);
    }
}