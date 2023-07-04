using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;

namespace Movies.Api.Sdk.Consumer;

public class AuthTokenProvider
{
    private readonly HttpClient _httpClient;
    private string _cachedToken = string.Empty;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public AuthTokenProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_cachedToken))
        {
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(_cachedToken);
            var expiryTimeText = jwt.Claims.Single(c => c.Type == "exp").Value;
            var expiryTime = UnixTimeStampToDateTime(int.Parse(expiryTimeText));
            if (expiryTime > DateTime.UtcNow)
            {
                return _cachedToken;
            }
        }

        await Lock.WaitAsync();

        var response = await _httpClient.PostAsJsonAsync("https://localhost:7185/token", new
        {
            userid = "7bd11c42-5dc3-4fac-87f6-e875fec5d56a",
            email = "tylerm@tyler-miller.net",
            customClaims = new Dictionary<string, object>
            {
                { "admin", true },
                { "trusted_member", true }
            }
        });

        var newToken = await response.Content.ReadAsStringAsync();
        _cachedToken = newToken;
        Lock.Release();
        return newToken;
    }
    
    private static DateTime UnixTimeStampToDateTime(int unixTimeStamp)
    {
        var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
        return dateTime;
    }
}