using System.Collections.Generic;

namespace Ocr.Api.Models
{
    public class DocTrOcrResult
    {
        public string Engine { get; set; } = "docTR";
        public string ImagePath { get; set; } = string.Empty;
        public string FullText { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public string JsonPath { get; set; } = string.Empty;
        public List<DocTrWordResult> Words { get; set; } = new();
    }

    public class DocTrWordResult
    {
        public string Text { get; set; } = string.Empty;
        public float Confidence { get; set; }
        public float XMin { get; set; }
        public float YMin { get; set; }
        public float XMax { get; set; }
        public float YMax { get; set; }
    }
}