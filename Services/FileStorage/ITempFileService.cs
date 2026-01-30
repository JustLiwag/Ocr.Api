
namespace Ocr.Api.Services.FileStorage
{
    public interface ITempFileService
    {
        Task<string> SaveFileAsync(IFormFile file);
    }

}
