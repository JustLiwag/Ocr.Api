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
        private readonly IPdfMergeService _pdfMergeService;
        private readonly IConfiguration _config;


        public OcrController(
            ITempFileService tempFileService,
            IPdfTextDetector pdfTextDetector,
            IPdfRenderService renderService,
            ITesseractService tesseractService,
            IPdfMergeService pdfMergeService,
            IConfiguration config)
        {
            _tempFileService = tempFileService;
            _pdfTextDetector = pdfTextDetector;
            _renderService = renderService;
            _tesseractService = tesseractService;
            _pdfMergeService = pdfMergeService;
            _config = config;
        }

        [HttpPost("manual")]
        public async Task<IActionResult> RunManualOcr(IFormFile file)
        {
            var baseDir = @"C:\Users\jeliwag\Downloads\OCR Test Data\results"; // custom folder
            var pdfPath = await _tempFileService.SaveFileAsync(file);

            if (_pdfTextDetector.HasText(pdfPath))
                return Ok("PDF already searchable.");

            var images = await _renderService.RenderAsync(pdfPath, baseDir, 300);
            bool useBest = true;

            var tessDataPath = useBest
                ? _config["Tesseract:Best"]
                : _config["Tesseract:Fast"];

            var pagePdfs = new List<string>();

            foreach (var image in images)
            {
                var pdf = await _tesseractService.RunOcrAsync(image, "eng", tessDataPath);
                Console.WriteLine($"OCR PDF created: {pdf}");
                pagePdfs.Add(pdf);
            }

            var mergedPdf = await _pdfMergeService.MergeAsync(pagePdfs, baseDir);


            return Ok(new
            {
                Pages = pagePdfs.Count,
                OutputPdf = mergedPdf
            });
    
        }
    }
}
