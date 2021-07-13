using Azure.Storage;
using Azure.Storage.Files.DataLake;
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
    public static class Function4
    {
        [FunctionName(nameof(Function4))]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            ILogger logger)
        {
            logger.LogInformation(
                $"{nameof(Function4)} trigger function processed a request.");

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

            // var credential = new DefaultAzureCredential();

            var credential =
                new StorageSharedKeyCredential(Environment.GetEnvironmentVariable("Function4BlobAccountName"), Environment.GetEnvironmentVariable("Function4BlobAccountKey"));

            // https://docs.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-directory-file-acl-dotnet
            // https://docs.microsoft.com/en-us/azure/storage/blobs/data-lake-storage-acl-dotnet

            var dataLakeServiceClient =
                new DataLakeServiceClient(new Uri(Environment.GetEnvironmentVariable("Function4BlobUri")), credential);

            var dataLakeFileSystemClient =
                dataLakeServiceClient.GetFileSystemClient(Environment.GetEnvironmentVariable("Function4BlobFilePath"));

            DataLakeDirectoryClient dataLakeDirectoryClient =
                await dataLakeFileSystemClient.CreateDirectoryAsync(DateTime.UtcNow.ToString("yyyyMMdd"));

            DataLakeFileClient dataLakeFileClient =
                await dataLakeDirectoryClient.CreateFileAsync(blobName);

            using (var fileStream = await file.ReadAsStreamAsync())
            {
                long fileSize = fileStream.Length;

                await dataLakeFileClient.AppendAsync(fileStream, offset: 0);

                await dataLakeFileClient.FlushAsync(position: fileSize);
            }

            return new OkObjectResult(new { name = blobName });
        }
    }
}
