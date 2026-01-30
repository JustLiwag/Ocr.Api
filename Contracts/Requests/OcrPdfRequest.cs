using Microsoft.AspNetCore.Http;

namespace Ocr.Api.Contracts.Requests
{
    public class OcrPdfRequest
    {
        // The uploaded PDF file
        public IFormFile File { get; set; } = default!;

        // Optional: OCR language code (default "eng")
        public string Language { get; set; } = "eng";

        // Optional: force OCR even if PDF already has text
        public bool ForceOcr { get; set; } = false;

        // Optional: target DPI for rendering images
        public int Dpi { get; set; } = 300;
    }
}
