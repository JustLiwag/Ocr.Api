using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Pdf;
using Ocr.Api.Services.Rendering;
using Ocr.Api.Services.Ocr;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Ocr.Api.Controllers
{
    [ApiController]
    [Route("api/ocr")]
    public class OcrController : ControllerBase
    {
        private readonly ITempFileService _tempFileService;
        private readonly IPdfTextDetector _pdfTextDetector;
        private readonly IPdfRenderService _renderService;
        private readonly ITesseractService _tesseractService;

        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
        }

        [HttpPost("manual")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var pdfPath = await _tempFileService.SaveFileAsync(file);

            if (_pdfTextDetector.HasText(pdfPath))
                return Ok("PDF already searchable.");

            var images = await _renderService.RenderAsync(pdfPath);
            var ocrResults = new List<string>();

            foreach (var image in images)
                ocrResults.Add(await _tesseractService.RunOcrAsync(image));

            return Ok(new
            {
                Pages = ocrResults.Count,
                OutputFiles = ocrResults
            });
        }
    }
}
