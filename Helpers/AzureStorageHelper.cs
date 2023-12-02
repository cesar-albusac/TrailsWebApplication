using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Trails.Models;

namespace TrailsWebApplication.Helpers
{
    public class AzureStorageHelper
    {
       
        
        // Upload file into Azure Blob storage
        public static async Task UploadFileToStorage(IFormFile file, Trail trail)
        {
            SecretClientOptions options = new SecretClientOptions()
            {
                Retry =
                {
                Delay= TimeSpan.FromSeconds(2),
                MaxDelay = TimeSpan.FromSeconds(16),
                MaxRetries = 5,
                Mode = RetryMode.Exponential
                }
            };
            SecretClient client = new SecretClient(new Uri("https://hikingtrailskeyvault.vault.azure.net/"), new DefaultAzureCredential(), options);

            KeyVaultSecret secret = client.GetSecret("trails-blob-connectionString");

            string secretValue = secret.Value;

            string container = Path.GetExtension(file.FileName) == ".gpx" ? "trails" : "images";

            var connectionString = secretValue;


            // intialize BobClient 
            BlobClient blobClient = new BlobClient(
                connectionString: connectionString,
                blobContainerName: container,
                blobName: file.FileName);

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                // Upload data from the local file
                await blobClient.UploadAsync(ms, true);
                var url = blobClient.Uri.AbsoluteUri;
                if (container == "trails")
                {
                    trail.GPXUrl = url;
                }
                else
                {
                    trail.ImageUrl =url;
                }
            }
        }

        // Upload file into Azure Blob storage
        public async static void DeleteFileFromStorage(string? file)
        {
            string container = Path.GetExtension(file) == ".gpx" ? "trails" : "images";

            var connectionString = "DefaultEndpointsProtocol=https;" +
                "AccountName=trailsstorageaccount;" +
                "AccountKey=" +
                "EndpointSuffix=core.windows.net";

            // intialize BobClient 
            BlobClient blobClient = new BlobClient(
                connectionString: connectionString,
                blobContainerName: container,
                blobName: file);

            BlobClient blobClientThumbnails = new BlobClient(
                connectionString: connectionString,
                blobContainerName: "thumbnails",
                blobName: file);

            await blobClient.DeleteIfExistsAsync();
            await blobClientThumbnails.DeleteIfExistsAsync();
           
        }
    }
}
