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
            var tempRoot = _tempRoot ?? Path.GetTempPath(); // make sure _tempRoot is valid
            Directory.CreateDirectory(tempRoot); // ensure folder exists

            var filePath = Path.Combine(tempRoot, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream); // write file
            }

            return filePath; // this must not be null
        }

    }

}
