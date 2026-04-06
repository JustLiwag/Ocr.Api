using OpenCvSharp;

namespace Ocr.Api.Services.ImageProcessing
{
    public class ImagePreprocessingService : IImagePreprocessingService
    {
        public string Preprocess(string imagePath, ImagePreprocessingOptions? options = null)
        {
            options ??= new ImagePreprocessingOptions();

            if (!options.Enabled)
                return imagePath;

            string outputPath = Path.Combine(
                Path.GetDirectoryName(imagePath)!,
                Path.GetFileNameWithoutExtension(imagePath) + "_processed.png"
            );

            if (!options.OverwriteIfExists && File.Exists(outputPath))
                return outputPath;

            using var img = Cv2.ImRead(imagePath, ImreadModes.Color);

            if (img.Empty())
                throw new FileNotFoundException("Could not load image for preprocessing.", imagePath);

            using var processed = ApplyProfile(img, options.Profile);

            Cv2.ImWrite(outputPath, processed);

            if (!File.Exists(outputPath))
                throw new FileNotFoundException("Processed image was not created.", outputPath);

            return outputPath;
        }

        private static Mat ApplyProfile(Mat img, string profile)
        {
            switch ((profile ?? "default").Trim().ToLowerInvariant())
            {
                case "none":
                    return img.Clone();

                case "light":
                    return ApplyLightProfile(img);

                case "safe":
                    return ApplySafeProfile(img);

                case "default":
                default:
                    return ApplyDefaultProfile(img);
            }
        }

        private static Mat ApplyDefaultProfile(Mat img)
        {
            var output = new Mat();
            double alpha = 1.2;
            int beta = -20;

            img.ConvertTo(output, -1, alpha, beta);
            return output;
        }

        private static Mat ApplyLightProfile(Mat img)
        {
            var output = new Mat();
            double alpha = 1.1;
            int beta = -10;

            img.ConvertTo(output, -1, alpha, beta);
            return output;
        }

        private static Mat ApplySafeProfile(Mat img)
        {
            using var adjusted = new Mat();
            using var gray = new Mat();
            var output = new Mat();

            double alpha = 1.1;
            int beta = -10;

            img.ConvertTo(adjusted, -1, alpha, beta);
            Cv2.CvtColor(adjusted, gray, ColorConversionCodes.BGR2GRAY);
            Cv2.MedianBlur(gray, output, 3);

            return output;
        }
    }
}