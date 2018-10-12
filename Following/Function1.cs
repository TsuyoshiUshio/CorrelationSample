
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
namespace Following
{
    public class Message
    {
        public string RootId { get; set; }
        public string ParentId { get; set; }
        public string Payload { get; set; }


    }

    // For Function App 2
    public static class ProcessQueues
    {
        private static TelemetryClient telemetryClient = new TelemetryClient(TelemetryConfiguration.Active);

        [FunctionName("FirstQueues")]
        public async static Task DequeueOne([QueueTrigger("myqueue-items", Connection = "QueueConnectionString")]
            Message myQueueItem
            , [Queue("secondqueue-items", Connection = "QueueConnectionString")]
            IAsyncCollector<Message> payloads, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            var telemetry = new RequestTelemetry
            {
                Name = "Dequeue " + "myqueue-items"
            };
            telemetry.Start();
            telemetry.Context.Operation.Id = myQueueItem.RootId;
            telemetry.Context.Operation.ParentId = myQueueItem.ParentId;
            telemetry.Stop();
            telemetryClient.Track(telemetry);

            var enqueueOperation =
                telemetryClient.StartOperation<DependencyTelemetry>("Enqueue " + "secondqueue-items");
            enqueueOperation.Telemetry.Type = "Queue";
            enqueueOperation.Telemetry.Data = "Enqueue " + "secondqueue-items";
            enqueueOperation.Telemetry.Context.Operation.Id = telemetry.Context.Operation.Id;
            enqueueOperation.Telemetry.Context.Operation.ParentId = telemetry.Id;
            var message = new Message
            {
                RootId = enqueueOperation.Telemetry.Context.Operation.Id,
                ParentId = enqueueOperation.Telemetry.Id,
                Payload = "hello",
            };
            await payloads.AddAsync(message);
            telemetryClient.StopOperation(enqueueOperation);

        }

        [FunctionName("SecondQueues")]
        public static void DequeueTwo([QueueTrigger("secondqueue-items", Connection = "QueueConnectionString")]
            Message myQueueItem, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {myQueueItem}");
            var telemetry = new RequestTelemetry
            {
                Name = "Dequeue " + "secondqueue-items"
            };
            telemetry.Start();
            telemetry.Context.Operation.Id = myQueueItem.RootId;
            telemetry.Context.Operation.ParentId = myQueueItem.ParentId;

            telemetry.Stop();
            telemetryClient.Track(telemetry);
        }
    }
}
