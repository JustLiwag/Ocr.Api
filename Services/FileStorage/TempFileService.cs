namespace Ocr.Api.Services.FileStorage
{
    public class TempFileService : ITempFileService
    {
        private readonly string _tempRoot;

        public TempFileService(IConfiguration config)
        {
            _tempRoot = config.GetValue<string>("OcrSettings:TempRoot")
                ?? Path.GetTempPath();
        }

        public async Task<string> SaveFileAsync(IFormFile file)
        {
            var fileName = Path.GetRandomFileName() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(_tempRoot, fileName);

            Directory.CreateDirectory(_tempRoot);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            return filePath;
        }
    }

}
