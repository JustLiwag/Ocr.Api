using Ocr.Api.Models;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Ocr.Api.Services.Pdf
{
    public class PdfSharpSearchablePdfBuilderService : ISearchablePdfBuilderService
    {
        public Task<string> BuildPagePdfAsync(
            string imagePath,
            IEnumerable<DocTrWordResult> words,
            string outputPdfPath)
        {
            if (!File.Exists(imagePath))
                throw new FileNotFoundException("Source image not found.", imagePath);

            var wordList = words?.ToList() ?? new List<DocTrWordResult>();

            using var document = new PdfDocument();
            var page = document.AddPage();

            using var image = XImage.FromFile(imagePath);

            page.Width = image.PointWidth;
            page.Height = image.PointHeight;

            using var gfx = XGraphics.FromPdfPage(page);

            // Draw the scanned image as the visible page background
            gfx.DrawImage(image, 0, 0, page.Width, page.Height);

            // Draw invisible/selectable text using docTR normalized coordinates
            foreach (var word in wordList)
            {
                if (string.IsNullOrWhiteSpace(word.Text))
                    continue;

                double x = word.XMin * page.Width;
                double y = word.YMin * page.Height;
                double width = (word.XMax - word.XMin) * page.Width;
                double height = (word.YMax - word.YMin) * page.Height;

                if (width <= 0 || height <= 0)
                    continue;

                double fontSize = height > 1 ? height : 10;

                var font = new XFont("Times New Roman", fontSize, XFontStyleEx.Regular);
                var brush = XBrushes.Transparent;

                gfx.DrawString(
                    word.Text,
                    font,
                    brush,
                    new XRect(x, y, width, height),
                    XStringFormats.TopLeft
                );
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputPdfPath)!);
            document.Save(outputPdfPath);

            if (!File.Exists(outputPdfPath))
                throw new FileNotFoundException("Searchable PDF page was not created.", outputPdfPath);

            return Task.FromResult(outputPdfPath);
        }
    }
}