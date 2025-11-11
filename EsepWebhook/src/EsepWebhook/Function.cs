using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook
{
    public class Function
    {
        // Lambda handler should be async for HTTP
        public async Task<string> FunctionHandler(string input, ILambdaContext context)
        {
            // Try parsing as unwrapped
            dynamic json;
            try
            {
                json = JsonConvert.DeserializeObject<dynamic>(input);
            }
            catch
            {
                // fallback: try parsing as root object with "body" property
                var jObj = JsonConvert.DeserializeObject<JObject>(input);
                if (jObj.ContainsKey("body"))
                {
                    json = JsonConvert.DeserializeObject<dynamic>(jObj["body"].ToString());
                }
                else
                {
                    throw; // no recognizable format
                }
            }

            // Defensive: The payload might be nested (GitHub webhook "body" wrapping)
            string issueUrl = null;
            try
            {
                issueUrl = json.issue.html_url.ToString();
            }
            catch
            {
                // Try as "body"
                if (json.body != null && json.body.issue != null)
                    issueUrl = json.body.issue.html_url.ToString();
            }

            if (string.IsNullOrEmpty(issueUrl))
                issueUrl = "No issue URL found";

            // Build Slack message
            string payload = $"{{\"text\": \"Issue Created: {issueUrl}\"}}";

            var client = new HttpClient();
            var webRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("SLACK_URL"))
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            var response = await client.SendAsync(webRequest);
            string responseText = await response.Content.ReadAsStringAsync();

            return responseText;
        }
    }
}
