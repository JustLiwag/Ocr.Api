using Ocr.Api.Models;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public interface ITesseractService
    {
        Task<OcrResult> RunOcrAsync(
    string imagePath,
    string lang,
    string tessDataDir);
    }
}
