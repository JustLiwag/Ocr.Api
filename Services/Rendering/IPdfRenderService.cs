using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Rendering
{
    public interface IPdfRenderService
    {
        Task<List<string>> RenderAsync(string pdfPath, string baseDir, int dpi = 300);
    }
}
