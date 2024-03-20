using Azure.Storage.Blobs;
using Trails.Models;

namespace TrailsWebApplication.Helpers
{
    public class AzureStorageHelper
    {
        // Upload file into Azure Blob storage
        public static async Task UploadFileToStorage(IFormFile file, Trail trail, string blobConnectionString, string container)
        {
            // intialize BobClient 
            BlobClient blobClient = new BlobClient(
                connectionString: blobConnectionString,
                blobContainerName: container,
                blobName: file.FileName);

            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms);
                ms.Seek(0, SeekOrigin.Begin);

                // Upload data from the local file
                await blobClient.UploadAsync(ms, true);
                var url = blobClient.Uri.AbsoluteUri;
                if (string.Equals(container,"trails"))
                {
                    trail.GPXUrl = url;
                }
                else
                {
                    trail.ImageUrl = url;
                }
            }
        }

        // Upload file into Azure Blob storage
        public async static void DeleteFileFromStorage(string? file, string blobConnectionString, string container)
        {
            // intialize BobClient 
            BlobClient blobClient = new BlobClient(
                connectionString: blobConnectionString,
                blobContainerName: container,
                blobName: file);

            BlobClient blobClientThumbnails = new BlobClient(
                connectionString: blobConnectionString,
                blobContainerName: "thumbnails",
                blobName: file);

            await blobClient.DeleteIfExistsAsync();
            await blobClientThumbnails.DeleteIfExistsAsync();
        }
    }
}
