using Movies.Api.Auth;
using Movies.Application.Services;
using Movies.Contracts.Requests;

namespace Movies.Api.Endpoints.Ratings;

public static class RateMovieEndpoint
{
    public const string Name = "RateMovie";

    public static IEndpointRouteBuilder MapRateMovie(this IEndpointRouteBuilder app)
    {
        app.MapPut(ApiEndpoints.Movies.Rate, async (Guid id, RateMovieRequest request, IRatingService ratingService, HttpContext httpContext, CancellationToken token) =>
            {
                var userId = httpContext.GetUserId();
                var result = await ratingService.RateMovieAsync(id, request.Rating, userId!.Value, token);
                return result ? Results.Ok() : Results.NotFound();

            })
            .WithName(Name);

        return app;
    }
}