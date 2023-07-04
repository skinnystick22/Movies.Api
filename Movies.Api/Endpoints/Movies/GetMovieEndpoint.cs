﻿using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Responses;

namespace Movies.Api.Endpoints.Movies;

public static class GetMovieEndpoint
{
    public const string Name = "GetMovie";

    public static IEndpointRouteBuilder MapGetMovie(this IEndpointRouteBuilder app)
    {
        app.MapGet(ApiEndpoints.Movies.Get,
                async (string idOrSlug,
                    IMovieService movieService,
                    HttpContext httpContext,
                    CancellationToken token) =>
                {
                    var userId = httpContext.GetUserId();

                    var movie = Guid.TryParse(idOrSlug, out var id)
                        ? await movieService.GetByIdAsync(id, userId, token)
                        : await movieService.GetBySlugAsync(idOrSlug, userId, token);

                    if (movie is null)
                    {
                        return Results.NotFound();
                    }

                    var response = movie.MapToResponse();
                    return Results.Ok(response);
                })
            .WithName(Name)
            .Produces<MovieResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .CacheOutput("MovieCache");

        return app;
    }
}