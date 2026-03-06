namespace Ocr.Api.Services.ImageProcessing
{
    public interface IImagePreprocessingService
    {
        string Preprocess(string imagePath);
    }
}