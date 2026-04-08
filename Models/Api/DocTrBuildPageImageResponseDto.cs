namespace Ocr.Api.Models.Api
{
    public class DocTrBuildPageImageResponseDto
    {
        public string Engine { get; set; } = "docTR";
        public string ImagePath { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string FullText { get; set; } = string.Empty;
        public int WordCount { get; set; }
        public string JsonPath { get; set; } = string.Empty;
        public string OutputPdf { get; set; } = string.Empty;
    }
}