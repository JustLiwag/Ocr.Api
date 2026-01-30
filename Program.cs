using Ocr.Api.Services.FileStorage;
using Ocr.Api.Services.Pdf;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Register TempFileService
builder.Services.AddScoped<ITempFileService, TempFileService>();
// Register PdfAnalysisService
builder.Services.AddScoped<IPdfAnalysisService, PdfAnalysisService>();

// Swagger for testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
