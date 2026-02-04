namespace Ocr.Api.Services.ImageProcessing
{
    public interface IImagePreprocessor
    {
        string Preprocess(string imagePath);
    }
}
