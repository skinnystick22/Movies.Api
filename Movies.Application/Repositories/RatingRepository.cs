using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class RatingRepository : IRatingRepository
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public RatingRepository(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task<bool> RateMovieAsync(Guid movieId, byte rating, Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);
        var result = await connection.ExecuteAsync(new CommandDefinition("""
        MERGE INTO Rating as TARGET
        USING (SELECT @MovieId as MovieId, @Rating as Rating, @UserId as UserId) as SOURCE
        ON (TARGET.MovieId = SOURCE.MovieId AND TARGET.UserId = SOURCE.UserId)
        WHEN MATCHED THEN
            UPDATE
            SET Rating = SOURCE.Rating
        WHEN NOT MATCHED THEN
            INSERT (MovieId, Rating, UserId)
            VALUES (SOURCE.MovieId, SOURCE.Rating, SOURCE.UserId);
        """, new { MovieId = movieId, Rating = rating, UserId = userId }, cancellationToken: cancellationToken));

        return result > 0;
    }

    public async Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<float?>(new CommandDefinition("""
        SELECT AVG(Rating)
        FROM Rating
        WHERE MovieId = @MovieId
        """, new { MovieId = movieId }, cancellationToken: cancellationToken));
    }

    public async Task<(float? Rating, byte? UserRating)> GetRatingAsync(Guid movieId, Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<(float?, byte?)>(new CommandDefinition("""
        SELECT AVG(Rating), 
               (SELECT TOP(1) Rating FROM Rating WHERE MovieId = @MovieId AND UserId = @UserId)
        FROM Rating
        WHERE MovieId = @MovieId
        """, new { MovieId = movieId, UserId = userId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);

        var result = await connection.ExecuteAsync(new CommandDefinition("""
        DELETE FROM Rating
        WHERE MovieId = @MovieId AND UserId = @UserId
        """, new { MovieId = movieId, UserId = userId }, cancellationToken: cancellationToken));

        return result > 0;
    }

    public async Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId,
        CancellationToken cancellationToken = default)
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken);
        return await connection.QueryAsync<MovieRating>(new CommandDefinition("""
        SELECT M.Id as MovieId, M.Slug, R.Rating
        FROM Rating as R
            JOIN Movie as M on M.Id = R.MovieId
        WHERE R.UserId = @UserId
        """, new { UserId = userId }, cancellationToken: cancellationToken));
    }
}