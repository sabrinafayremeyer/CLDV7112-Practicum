using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace GameStop.Functions.Functions;

public class GameImageProcessor(ILogger<GameImageProcessor> logger, IConfiguration config)
{
    [Function("GameImageProcessor")]
    public async Task Run(
        [BlobTrigger("game-images/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name)
    {
        logger.LogInformation("Processing image: {Name}", name);

        try
        {
            // resize to thumbnail
            using var image = await Image.LoadAsync(blobStream);
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(300, 400),
                Mode = ResizeMode.Crop
            }));

            var connStr        = config["AzureWebJobsStorage"];
            var thumbContainer = config["ThumbnailContainerName"] ?? "game-thumbnails";

            var thumbContainerClient = new BlobContainerClient(connStr, thumbContainer);
            await thumbContainerClient.CreateIfNotExistsAsync();
            var thumbClient = thumbContainerClient.GetBlobClient(name);

            using var ms = new MemoryStream();
            await image.SaveAsJpegAsync(ms);
            ms.Position = 0;
            await thumbClient.UploadAsync(ms, overwrite: true);

            // write audit log
            var tableConn  = config["AzureWebJobsStorage"];
            var tableName  = config["AuditTableName"] ?? "AuditLogs";
            var tableClient = new TableClient(tableConn, tableName);
            await tableClient.CreateIfNotExistsAsync();

            await tableClient.AddEntityAsync(new TableEntity("ImageUpload", Guid.NewGuid().ToString())
            {
                ["FileName"]    = name,
                ["Status"]      = "ThumbnailCreated",
                ["ProcessedAt"] = DateTime.UtcNow.ToString("O")
            });

            logger.LogInformation("Thumbnail created and audit log written for {Name}", name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process image {Name}", name);
        }
    }
}
