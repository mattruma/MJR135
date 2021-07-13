using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
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
    public static class Function2
    {
        [FunctionName(nameof(Function2))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            ILogger logger,
            [Blob("%Function2BlobFilePath%", FileAccess.Write, Connection = "Function2BlobConnectionString")] CloudBlobContainer cloudBlobContainer)
        {
            logger.LogInformation(
                $"{nameof(Function2)} trigger function processed a request.");

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

            var cloudBlockBlob =
                cloudBlobContainer.GetBlockBlobReference(blobName);

            cloudBlockBlob.Properties.ContentType =
               file.Headers.ContentType.MediaType;

            using (var fileStream = await file.ReadAsStreamAsync())
            {
                await cloudBlockBlob.UploadFromStreamAsync(fileStream);
            }

            return new OkObjectResult(new { name = blobName });
        }
    }
}
