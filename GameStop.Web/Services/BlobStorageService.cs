using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace GameStop.Web.Services;

public class BlobStorageService
{
    private readonly BlobServiceClient _client;
    private readonly string _container;

    public BlobStorageService(IConfiguration config)
    {
        _client = new BlobServiceClient(config["AzureStorage:ConnectionString"]);
        _container = config["AzureStorage:BlobContainer"] ?? "game-images";
    }

    public async Task<string> UploadImageAsync(Stream stream, string fileName, string contentType)
    {
        var container = _client.GetBlobContainerClient(_container);
        await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
        var blob = container.GetBlobClient(fileName);
        await blob.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } });
        return blob.Uri.ToString();
    }
}
