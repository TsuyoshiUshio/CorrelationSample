
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http.Extensions;

namespace Correlation
{
    public class Message
    {
        public string RootId { get; set; }
        public  string ParentId { get; set; }
        public string Payload { get; set; }
              
        
    }

    // For Function App 1
    public static class MultipleQueues
    {

        private static TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

        [FunctionName("HttpTrigger")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequest req
            , [Queue("myqueue-items", Connection = "AzureWebJobsStorage")] IAsyncCollector<Message> payloads
            , ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // 1. Receive HttpRequest 
            var requestTelemetry = new RequestTelemetry
            {
                Name = $"{req.Method} {req.Path}"
            };
            if (req.Headers.ContainsKey("Request-Id"))
            {
                var requestId = req.Headers["Request-Id"];
                requestTelemetry.Context.Operation.Id = GetOperationId("Request-Id");
                requestTelemetry.Context.Operation.ParentId = requestId;
                requestTelemetry.Context.Cloud.RoleName = "sadistribute1011app";

                log.LogInformation($"RequestId found: {requestId}");
            }

            var operation = telemetryClient.StartOperation(requestTelemetry);

            requestTelemetry.Success = true;
            telemetryClient.StopOperation(operation);

            // 2. Enqueue 
            var enqueueOperation = telemetryClient.StartOperation <DependencyTelemetry>("Enqueue " + "myqueue-items");
            enqueueOperation.Telemetry.Type = "Queue";
            enqueueOperation.Telemetry.Data = "Enqueue " + "myqueue-items";
            enqueueOperation.Telemetry.Context.Operation.Id = requestTelemetry.Context.Operation.Id;
            enqueueOperation.Telemetry.Context.Operation.ParentId = requestTelemetry.Id;
            enqueueOperation.Telemetry.Context.Cloud.RoleName = "sadistribute1011app";

            var message = new Message
            {
                RootId = enqueueOperation.Telemetry.Context.Operation.Id,
                ParentId = enqueueOperation.Telemetry.Id,
                Payload = "hello",
            };
            await payloads.AddAsync(message);
            telemetryClient.StopOperation(enqueueOperation);

            var enqueueOperationJson = JsonConvert.SerializeObject(enqueueOperation);
            log.LogInformation("EnqueueOperationJson:");
            log.LogInformation(enqueueOperationJson);

            return new OkObjectResult("Queue has been sent.");
        }

        public static string GetOperationId(string id)
        {
            // Return the root ID from the '|' to the first '.' if any.
            int rootEnd = id.IndexOf('.');
            if (rootEnd < 0)          
                rootEnd = id.Length;

            int rootStart = id[0] == '|' ? 1 : 0;
            return id.Substring(rootStart, rootEnd - rootStart);            
        }


        //[FunctionName("FirstQueues")]
        //public async static Task DequeueOne([QueueTrigger("myqueue-items", Connection = "AzureWebJobsStorage")]Message myQueueItem
        //    , [Queue("secondqueue-items", Connection = "AzureWebJobsStorage")] IAsyncCollector<Message> payloads, ILogger log)
        //{
        //    log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        //    var telemetry = new RequestTelemetry
        //    {
        //        Name = "Dequeue " + "myqueue-items"
        //    };
        //    telemetry.Start();
        //    telemetry.Context.Operation.Id = myQueueItem.RootId;
        //    telemetry.Context.Operation.ParentId = myQueueItem.ParentId;
        //    telemetry.Stop();
        //    telemetryClient.Track(telemetry);

        //    var enqueueOperation = telemetryClient.StartOperation<DependencyTelemetry>("Enqueue " + "secondqueue-items");
        //    enqueueOperation.Telemetry.Type = "Queue";
        //    enqueueOperation.Telemetry.Data = "Enqueue " + "secondqueue-items";
        //    enqueueOperation.Telemetry.Context.Operation.Id = telemetry.Context.Operation.Id;
        //    enqueueOperation.Telemetry.Context.Operation.ParentId = telemetry.Id;
        //    var message = new Message
        //    {
        //        RootId = enqueueOperation.Telemetry.Context.Operation.Id,
        //        ParentId = enqueueOperation.Telemetry.Id,
        //        Payload = "hello",
        //    };
        //    await payloads.AddAsync(message);
        //    telemetryClient.StopOperation(enqueueOperation);

        //}

        //[FunctionName("SecondQueues")]
        //public static void DequeueTwo([QueueTrigger("secondqueue-items", Connection = "AzureWebJobsStorage")]Message myQueueItem, ILogger log)
        //{
        //    log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
        //    var telemetry = new RequestTelemetry
        //    {
        //        Name = "Dequeue " + "secondqueue-items"
        //    };
        //    telemetry.Start();
        //    telemetry.Context.Operation.Id = myQueueItem.RootId;
        //    telemetry.Context.Operation.ParentId = myQueueItem.ParentId;
            
        //    telemetry.Stop();
        //    telemetryClient.Track(telemetry);
        //}
    }
}
