using OpenCvSharp;

namespace Ocr.Api.Services.ImageProcessing
{
    public class ImagePreprocessingService : IImagePreprocessingService
    {
        public string Preprocess(string imagePath)
        {
            var img = Cv2.ImRead(imagePath);

            var adjusted = new Mat();

            // alpha = contrast (1.0 = normal)
            // beta  = brightness (0 = normal)
            double alpha = 1.2;   // Increase contrast slightly
            int beta = -20;       // Reduce brightness slightly

            img.ConvertTo(adjusted, -1, alpha, beta);

            string outputPath = Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath) + "_processed.png"
            );

            Cv2.ImWrite(outputPath, adjusted);

            return outputPath;
        }
    }
}