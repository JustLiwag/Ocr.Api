using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Rendering
{
    public interface IPdfRenderService
    {
        Task<List<string>> RenderAsync(string pdfPath, string baseDir, int dpi = 300);

        Task<int> GetPageCountAsync(string pdfPath);

        Task<string> RenderPageAsync(
            string pdfPath,
            string baseDir,
            int pageNumber,
            int dpi = 300);

        Task<List<string>> RenderPagesAsync(
            string pdfPath,
            string baseDir,
            int startPage,
            int endPage,
            int dpi = 300);
    }
}