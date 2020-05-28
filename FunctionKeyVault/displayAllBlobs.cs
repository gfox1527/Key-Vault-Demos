using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Azure.Storage.Blobs;
using Azure.Identity;

namespace FunctionKeyVault
{
    public static class displayAllBlobs
    {
        private static IConfiguration Configuration { set; get; }

        static displayAllBlobs()
        {
            //preferred methods to save connection strings locally is with environment variables or
            //local user-secrets cache instead of embedding them in local.settings.json
            //
            //  dotnet user-secrets set ConnectionString:AppConfig <your_connection_string>
            //  builder.AddAzureAppConfiguration(settings["ConnectionString:AppConfig"]);
            //
            //  setx ConnectionString:AppConfig <your_connection_string>
            //  builder.AddAzureAppConfiguration(Environment.GetEnvironmentVariable("ConnectionString:AppConfig"));
            //
            //this function uses user-secrets variables
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .AddAzureAppConfiguration(options =>
                {
                    options.Connect(Environment.GetEnvironmentVariable($"ConnectionString:AppConfig"))
                            .ConfigureKeyVault(kv =>
                            {
                                kv.SetCredential(new DefaultAzureCredential());
                            });
                });
            Configuration = builder.Build();
        }

        [FunctionName("displayAllBlobs")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("FunctionKeyVault.displayBlobs function processed a request.");

            BlobServiceClient blobServiceClient = new BlobServiceClient(Configuration["TestApp:Settings:StgConnString"]);
            BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(Configuration["TestApp:Settings:StgContainerName"]);
            int blobCount = 0;
            foreach (var blob in containerClient.GetBlobs())
            {
                blobCount++;
            }

            string responseMessage = "Found " + blobCount.ToString() + " blobs in storage account " + containerClient.Uri.ToString() +
            " in container " + containerClient.Name.ToString();

            return new OkObjectResult(responseMessage);
        }
    }
}