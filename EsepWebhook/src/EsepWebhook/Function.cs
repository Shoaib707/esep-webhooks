using System;
using System.Net.Http;
using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace EsepWebhook

{
    public class Function
    {
        public string FunctionHandler(object input, ILambdaContext context)
        {
            try
            {
                dynamic json = JsonConvert.DeserializeObject<dynamic>(input.ToString());

                // Defensive null check
                if (json?.issue?.html_url == null)
                {
                    context.Logger.LogLine("Missing issue.html_url in payload.");
                    return "Issue URL not found in payload.";
                }

                string issueUrl = json.issue.html_url;
                var payloadObj = new { text = $"New GitHub Issue Created: {issueUrl}" };
                string payload = JsonConvert.SerializeObject(payloadObj);
                
                context.Logger.LogLine($"Parsed Issue URL: {issueUrl}");
                context.Logger.LogLine($"Payload: {payload}");

                // Post to Slack
                var slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
                if (string.IsNullOrEmpty(slackUrl))
                {
                    context.Logger.LogLine("SLACK_URL env variable is missing.");
                    return "Slack URL missing";
                }

                using var client = new HttpClient();
                var content = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = client.PostAsync(slackUrl, content).Result;

                return response.IsSuccessStatusCode ? "Posted to Slack" : $"Slack error: {response.StatusCode}";
            }
            catch (Exception ex)
            {
                context.Logger.LogLine($"Error: {ex.Message}");
                return $"Error: {ex.Message}";
            }
        }
    }
}
