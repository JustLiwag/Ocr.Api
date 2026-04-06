namespace Ocr.Api.Services.ImageProcessing
{
    public interface IImagePreprocessingService
    {
        string Preprocess(string imagePath, ImagePreprocessingOptions? options = null);
    }

    public class ImagePreprocessingOptions
    {
        public bool Enabled { get; set; } = true;
        public string Profile { get; set; } = "default";
        public bool OverwriteIfExists { get; set; } = true;
    }
}