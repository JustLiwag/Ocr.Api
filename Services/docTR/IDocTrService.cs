using Ocr.Api.Models;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public interface IDocTrService
    {
        Task<DocTrOcrResult> RunOcrAsync(string imagePath);
    }
}