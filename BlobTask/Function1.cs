using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;


namespace BlobTask
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([BlobTrigger("images/{name}", Connection = "AzureWebJobsStorage")] Stream stream, string name, FunctionContext context)
        {
            var metadata = context.BindingContext.BindingData;
            if (metadata.TryGetValue("Uri", out var uri))
            {
                if (name.EndsWith(".png"))
                {
                    try
                    {
                        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
                        var blobServiceClient = new BlobServiceClient(connectionString);
                        var containerClient = blobServiceClient.GetBlobContainerClient("images");

                        using (var image = Image.Load(stream))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                var jpegEncoder = new JpegEncoder
                                {
                                    Quality = 10
                                };

                                image.Save(memoryStream, jpegEncoder);
                                memoryStream.Position = 0;

                                var blobClient = containerClient.GetBlobClient($"{Path.GetFileNameWithoutExtension(name)}.jpg");
                                await blobClient.UploadAsync(memoryStream, overwrite: true);

                                _logger.LogInformation($"Converted and uploaded {name} as JPG.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing {name}: {ex.Message}");
                    }
                }
                else
                {
                    _logger.LogInformation($"The file {name} is not a PNG. Skipping processing.");
                }
            }
        }
    }
}
