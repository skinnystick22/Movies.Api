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
}