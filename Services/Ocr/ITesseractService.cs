using System.Threading.Tasks;

namespace Ocr.Api.Services.Ocr
{
    public interface ITesseractService
    {
        Task<string> RunOcrAsync(
    string imagePath,
    string lang,
    string tessDataDir);
    }
}
