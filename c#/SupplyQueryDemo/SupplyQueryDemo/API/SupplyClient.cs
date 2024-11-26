using Nexar.Client.Token;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SupplyQueryDemo.API;

internal class SupplyClient : IDisposable
{
    // access tokens expire after one day
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromDays(1);

    // keep track of token and expiry time
    private static string? _token;
    private static DateTime _tokenExpiresAt = DateTime.MinValue;

    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly HttpClient _httpClient;

    public SupplyClient(string clientId, string clientSecret)
    {
        this._clientId = clientId;
        this._clientSecret = clientSecret;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://api.nexar.com/graphql")
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<Response?> RunQueryAsync(Request request)
    {
        // for another way of running GraphQL queries, see the related demo at:
        // https://github.com/NexarDeveloper/nexar-templates/tree/main/nexar-console-supply
        await EnsureValidTokenAsync();
        var requestString = JsonSerializer.Serialize(request);
        HttpResponseMessage httResponse = await _httpClient.PostAsync(_httpClient.BaseAddress, new StringContent(requestString, Encoding.UTF8, "application/json"));
        httResponse.EnsureSuccessStatusCode();
        var responseString = await httResponse.Content.ReadAsStringAsync();
        var response = JsonSerializer.Deserialize<Response?>(responseString);
        return response;
    }

    private async Task EnsureValidTokenAsync()
    {
        // get an access token, replacing the existing one if it has expired
        if (_token == null || DateTime.UtcNow >= _tokenExpiresAt)
        {
            _tokenExpiresAt = DateTime.UtcNow + TokenLifetime;
            using HttpClient authClient = new();
            _token = await authClient.GetNexarTokenAsync(_clientId, _clientSecret);
        }

        // set the default Authorization header so it includes the token
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
    }
}
