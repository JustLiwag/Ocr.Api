using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Ocr.Api.Services.FileStorage
{
    public interface ITempFileService
    {
        Task<string> SaveFileAsync(IFormFile file);
    }
}


