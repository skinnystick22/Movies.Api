using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Responses;

namespace Movies.Api.Endpoints.Ratings;

public static class GetUserRatingsEndpoint
{
    public const string Name = "GetUserRatings";

    public static IEndpointRouteBuilder MapGetUserRatings(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Ratings.GetUserRatings, async (IRatingService ratingService,
            HttpContext httpContext,
            CancellationToken token) =>
        {
            var userId = httpContext.GetUserId();
            var ratings = await ratingService.GetRatingsForUserAsync(userId!.Value, token);
            var ratingResponse = ratings.MapToResponse();

            return TypedResults.Ok(ratingResponse);
        })
            .WithName(Name)
            .Produces<MovieRatingsResponse>(StatusCodes.Status200OK)
            
            .RequireAuthorization();

        return app;
    }
}