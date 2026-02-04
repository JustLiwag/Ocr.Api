using ImageMagick;

namespace Ocr.Api.Services.ImageProcessing
{
    public class ImageMagickPreprocessor : IImagePreprocessor
    {
        public string Preprocess(string imagePath)
        {
            var dir = Path.GetDirectoryName(imagePath)!;
            var fileName = Path.GetFileName(imagePath);
            var outputPath = Path.Combine(dir, "pre_" + fileName);

            using var image = new MagickImage(imagePath);

            // 🔥 OCR-focused preprocessing
            image.Deskew((Percentage)40);
            //image.Grayscale();
            //image.ContrastStretch((Percentage)0.1, (Percentage)0.1);
            //image.Threshold(new Percentage(85));
            //image.Sharpen();

            image.Write(outputPath);
            return outputPath;
        }
    }
}
