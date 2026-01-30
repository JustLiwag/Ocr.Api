using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Rendering
{
    public interface IPdfRenderService
    {
        Task<List<string>> RenderAsync(string pdfPath, int dpi = 300);
    }
}
