using Newtonsoft.Json;
using Nexar.Client.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SupplyQueryDemo
{
    internal static class SupplyClient
    {
        // access tokens expire after one day
        private static readonly TimeSpan tokenLifetime = TimeSpan.FromDays(1);

        // assume Nexar client ID and secret are set as environment variables
        private static readonly string clientId = Environment.GetEnvironmentVariable("NEXAR_CLIENT_ID") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_ID'");
        private static readonly string clientSecret = Environment.GetEnvironmentVariable("NEXAR_CLIENT_SECRET") ?? throw new InvalidOperationException("Please set environment variable 'NEXAR_CLIENT_SECRET'");

        // keep track of token and expiry time
        private static string? token = null;
        private static DateTime tokenExpiresAt = DateTime.MinValue;

        internal static async Task<HttpClient> GetClientAsync()
        {
            // get an access token, or get a new one if it expired
            if (token == null || DateTime.UtcNow >= tokenExpiresAt)
            {
                tokenExpiresAt = DateTime.UtcNow + tokenLifetime;
                using HttpClient authClient = new();
                token = await authClient.GetNexarTokenAsync(clientId, clientSecret);
            }

            // create and configure the supply client
            HttpClient supplyClient = new()
            {
                BaseAddress = new Uri("https://api.nexar.com/graphql"),
                DefaultRequestHeaders =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", token)
                }
            };

            return supplyClient;
        }

        internal static async Task<Response> RunQueryAsync(this HttpClient supplyClient, Request request)
        {
            string requestString = JsonConvert.SerializeObject(request);
            HttpResponseMessage httResponse = await supplyClient.PostAsync(supplyClient.BaseAddress, new StringContent(requestString, Encoding.UTF8, "application/json"));
            httResponse.EnsureSuccessStatusCode();
            string responseString = await httResponse.Content.ReadAsStringAsync();
            Response response = JsonConvert.DeserializeObject<Response>(responseString);
            return response;
        }
    }
}
