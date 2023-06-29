// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Movies.Api.Sdk;
using Movies.Contracts.Requests;
using Refit;

var services = new ServiceCollection();

services.AddRefitClient<IMoviesApi>(x => new RefitSettings
    {
        AuthorizationHeaderValueGetter = () => Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiJjNzU5ZDY1Yi02ZWZjLTRiOTktYWRjNi1mMjE2MmIwOWQ4NWUiLCJzdWIiOiJ0eWxlckB0eWxlci1taWxsZXIubmV0IiwiZW1haWwiOiJ0eWxlckB0eWxlci1taWxsZXIubmV0IiwidXNlcmlkIjoiN2JkMTFjNDItNWRjMy00ZmFjLTg3ZjYtZTg3NWZlYzVkNTZhIiwiYWRtaW4iOnRydWUsInRydXN0ZWRfbWVtYmVyIjp0cnVlLCJuYmYiOjE2ODgwMDY2NzEsImV4cCI6MTY4ODAzNTQ3MSwiaWF0IjoxNjg4MDA2NjcxLCJpc3MiOiJodHRwczovL2lkLm5pY2tjaGFwc2FzLmNvbSIsImF1ZCI6Imh0dHBzOi8vbW92aWVzLm5pY2tjaGFwc2FzLmNvbSJ9.PGItr8gWKqygloULD5m8H_FLE2AgL9nMT020ycwS25Y")
    })
    .ConfigureHttpClient(config =>
    {
        config.BaseAddress = new Uri("https://localhost:7210");
    });

var serviceProvider = services.BuildServiceProvider();

var moviesApi = serviceProvider.GetRequiredService<IMoviesApi>();

var movie = await moviesApi.GetMovieAsync("john-wick-2014");

var movies = await moviesApi.GetMoviesAsync(new GetAllMoviesRequest
{
    Page = 1,
    PageSize = 3,
    Title = null,
    YearOfRelease = null,
    SortBy = null
});

foreach (var movieResponse in movies.Items)
{
    Console.WriteLine(JsonSerializer.Serialize(movieResponse));    
}

Console.WriteLine(JsonSerializer.Serialize(movie));