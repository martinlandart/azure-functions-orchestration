using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Orchestrator
{
    public static class Orchestrator
    {
        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            var outputs = new List<string>();

            var tasks = new List<Task<string>>();

            tasks.Add(context.CallActivityAsync<string>("Orchestrator_Hello", "Tokyo"));
            tasks.Add(context.CallActivityAsync<string>("Orchestrator_Hello", "Seattle"));
            tasks.Add(context.CallActivityAsync<string>("Orchestrator_Hello", "London"));

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                outputs.Add(task.Result);
            }

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName("Orchestrator_Hello")]
        public static async Task<string> SayHello([ActivityTrigger] string name, ILogger log)
        {
            await Task.Delay(5000);
            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Orchestrator_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Orchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }
    }
}