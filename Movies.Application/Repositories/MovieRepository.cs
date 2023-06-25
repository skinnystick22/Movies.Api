using Dapper;
using Movies.Application.Database;
using Movies.Application.Models;

namespace Movies.Application.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public MovieRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync(new CommandDefinition("""
        INSERT INTO Movie (Id, Slug, Title, YearOfRelease)
        VALUES (@Id, @Slug, @Title, @YearOfRelease)
        """, movie, transaction, cancellationToken: cancellationToken));

        if (result > 0)
        {
            foreach (var movieGenre in movie.Genres)
            {
                await connection.ExecuteAsync(new CommandDefinition("""
                INSERT INTO Genre (MovieId, Name)
                VALUES (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = movieGenre }, transaction,
                    cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<Movie?> GetByIdAsync(Guid id, Guid? userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(new CommandDefinition("""
        SELECT Id, Slug, Title, YearOfRelease, AVG(r.Rating) as Rating, r2.Rating as UserRating
        FROM Movie AS m
        LEFT JOIN Rating as r on r.MovieId = m.Id
        LEFT JOIN Rating as r2 on r2.MovieId = m.Id and r2.UserId = @UserId
        WHERE Id = @Id
        GROUP BY r2.Rating, YearOfRelease, Title, Slug, Id
        """, new { Id = id, UserId = userId }, cancellationToken: cancellationToken));

        if (movie == null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
        SELECT Name
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug, Guid? userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(new CommandDefinition("""
        SELECT M.Id, M.Slug, M.Title, M.YearOfRelease, AVG(R.Rating) as Rating, MYR.Rating as UserRating
        FROM Movie AS M
            LEFT JOIN Rating as R on R.MovieId = M.Id
            LEFT JOIN Rating as MYR on MYR.MovieId = M.Id and MYR.UserId = @UserId
        WHERE Slug = @Slug
        GROUP BY MYR.Rating, YearOfRelease, Title, Slug, Id
        """, new { Slug = slug, UserId = userId }, cancellationToken: cancellationToken));

        if (movie == null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>(new CommandDefinition("""
        SELECT Name
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = movie.Id }, cancellationToken: cancellationToken));

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync(Guid? userId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var result = await connection.QueryAsync(new CommandDefinition("""
        SELECT M.Id, M.Title, M.YearOfRelease, STRING_AGG(G.Name, ', ') AS Genres, AVG(R.Rating) as Rating, MYR.Rating as UserRating
        FROM Movie as M
            LEFT JOIN Genre as G on M.Id = G.MovieId
            LEFT JOIN Rating as R on R.MovieId = M.Id
            LEFT JOIN Rating as MYR on MYR.MovieId = M.Id and MYR.UserId = @UserId
        GROUP BY M.Id, M.Title, M.YearOfRelease, MYR.Rating
        """, new { UserId = userId }, cancellationToken: cancellationToken));

        return result.Select(x => new Movie
        {
            Id = x.Id,
            Title = x.Title,
            YearOfRelease = x.YearOfRelease,
            Rating = (float?)x.Rating,
            UserRating = (byte?)x.UserRating,
            Genres = Enumerable.ToList(x.Genres.Split(","))
        });
    }

    public async Task<bool> UpdateAsync(Movie movie, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
        DELETE
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = movie.Id }, transaction, cancellationToken: cancellationToken));

        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO Genre (MovieId, Name)
            VALUES (@MovieId, @Name)
            """, new { MovieId = movie.Id, Name = genre.Trim() }, transaction, cancellationToken: cancellationToken));
        }

        var result = await connection.ExecuteAsync(new CommandDefinition("""
        UPDATE Movie
        SET Slug = @Slug, Title = @Title, YearOfRelease = @YearOfRelease
        WHERE Id = @Id
        """, movie, transaction, cancellationToken: cancellationToken));

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(new CommandDefinition("""
        DELETE
        FROM Genre
        where MovieId = @MovieId
        """, new { MovieId = id }, transaction, cancellationToken: cancellationToken));

        var result = await connection.ExecuteAsync(new CommandDefinition("""
        DELETE
        FROM Movie
        WHERE Id = @Id
        """, new { Id = id }, transaction, cancellationToken: cancellationToken));

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition("""
        SELECT COUNT(1)
        FROM Movie
        WHERE Id = @Id
        """, new { id }, cancellationToken: cancellationToken));
    }
}