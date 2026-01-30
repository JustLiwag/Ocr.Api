namespace Ocr.Api.Contracts.Responses
{
    public class OcrErrorResponse
    {
        // Short machine-friendly error code
        public string Error { get; set; } = default!;

        // Human-readable error message
        public string Message { get; set; } = default!;
    }
}
