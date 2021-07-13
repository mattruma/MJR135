using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FunctionApp1
{
    public static class Function3
    {
        [FunctionName(nameof(Function3))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            ILogger logger)
        {
            logger.LogInformation(
                $"{nameof(Function3)} trigger function processed a request.");

            var multipartMemoryStreamProvider =
                new MultipartMemoryStreamProvider();

            await req.Content.ReadAsMultipartAsync(multipartMemoryStreamProvider);

            var file =
                multipartMemoryStreamProvider.Contents.First();
            var fileInfo =
                file.Headers.ContentDisposition;

            logger.LogInformation(
                JsonConvert.SerializeObject(fileInfo, Formatting.Indented));

            var blobName =
                $"{Guid.NewGuid()}{Path.GetExtension(fileInfo.FileName)}";

            blobName = blobName.Replace("\"", "");

            var credential = new DefaultAzureCredential();

            var blobContainerClient = new BlobContainerClient(new Uri(Environment.GetEnvironmentVariable("Function3BlobUri")), credential);

            using (var fileStream = await file.ReadAsStreamAsync())
            {
                await blobContainerClient.UploadBlobAsync(blobName, fileStream);
            }

            return new OkObjectResult(new { name = blobName });
        }
    }
}
