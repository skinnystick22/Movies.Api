using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Movies.Api.Sdk;
using Movies.Api.Sdk.Consumer;
using Movies.Contracts.Requests;
using Refit;

var services = new ServiceCollection();

services
    .AddHttpClient()
    .AddSingleton<AuthTokenProvider>()
    .AddRefitClient<IMoviesApi>(sc => new RefitSettings
    {
        AuthorizationHeaderValueGetter = async () => await sc.GetRequiredService<AuthTokenProvider>().GetTokenAsync()
    })
    .ConfigureHttpClient(config =>
    {
        config.BaseAddress = new Uri("https://localhost:7210");
    });

var serviceProvider = services.BuildServiceProvider();

var moviesApi = serviceProvider.GetRequiredService<IMoviesApi>();

var movie = await moviesApi.GetMovieAsync("john-wick-2014");

var newMovie = await moviesApi.CreateMovieAsync(new CreateMovieRequest
{
    Title = "Happy Death Day",
    YearOfRelease = 2017,
    Genres = new []
    {
        "Horror"
    }
});

await moviesApi.UpdateMovieAsync(newMovie.Id, new UpdateMovieRequest
{
    Title = newMovie.Title,
    YearOfRelease = newMovie.YearOfRelease,
    Genres = new[]
    {
        "Horror",
        "Comedy"
    },
});

await moviesApi.DeleteMovieAsync(newMovie.Id);

var movies = await moviesApi.GetMoviesAsync(new GetAllMoviesRequest
{
    Page = 1,
    PageSize = 25,
    Title = null,
    YearOfRelease = null,
    SortBy = null
});

foreach (var movieResponse in movies.Items)
{
    Console.WriteLine(JsonSerializer.Serialize(movieResponse));    
}