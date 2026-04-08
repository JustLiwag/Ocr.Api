namespace Ocr.Api.Models.Records
{
    public class DocTrNormalizedToken
    {
        public string RawText { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string TokenType { get; set; } = "Word";
        public float Confidence { get; set; }
        public float XMin { get; set; }
        public float YMin { get; set; }
        public float XMax { get; set; }
        public float YMax { get; set; }
    }
}