using System.Dynamic;
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

    public async Task<bool> CreateAsync(Movie movie)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        using var transaction = connection.BeginTransaction();

        var result = await connection.ExecuteAsync("""
        INSERT INTO Movie (Id, Slug, Title, YearOfRelease)
        VALUES (@Id, @Slug, @Title, @YearOfRelease)
        """, movie, transaction);

        if (result > 0)
        {
            foreach (var movieGenre in movie.Genres)
            {
                await connection.ExecuteAsync("""
                INSERT INTO Genre (MovieId, Name)
                VALUES (@MovieId, @Name)
                """, new { MovieId = movie.Id, Name = movieGenre }, transaction);
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<Movie?> GetByIdAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>("""
        SELECT Id, Slug, Title, YearOfRelease
        FROM Movie
        WHERE Id = @Id
        """, new { Id = id });

        if (movie == null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>("""
        SELECT Name
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = id });

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<Movie?> GetBySlugAsync(string slug)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>("""
        SELECT Id, Slug, Title, YearOfRelease
        FROM Movie
        WHERE Slug = @Slug
        """, new { Slug = slug });

        if (movie == null)
        {
            return null;
        }

        var genres = await connection.QueryAsync<string>("""
        SELECT Name
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = movie.Id });

        foreach (var genre in genres)
        {
            movie.Genres.Add(genre);
        }

        return movie;
    }

    public async Task<IEnumerable<Movie>> GetAllAsync()
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        var result = await connection.QueryAsync("""
        SELECT M.Id, M.Title, M.YearOfRelease, STRING_AGG(G.Name, ', ') AS Genres
        FROM Movie M
            LEFT JOIN Genre G on M.Id = G.MovieId
        GROUP BY M.Id, M.Title, M.YearOfRelease
        """);

        return result.Select(x => new Movie
        {
            Id = x.Id,
            Title = x.Title,
            YearOfRelease = x.YearOfRelease,
            Genres = Enumerable.ToList(x.Genres.Split(","))
        });
    }

    public async Task<bool> UpdateAsync(Movie movie)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync("""
        DELETE
        FROM Genre
        WHERE MovieId = @MovieId
        """, new { MovieId = movie.Id }, transaction);

        foreach (var genre in movie.Genres)
        {
            await connection.ExecuteAsync("""
            INSERT INTO Genre (MovieId, Name)
            VALUES (@MovieId, @Name)
            """, new { MovieId = movie.Id, Name = genre.Trim() }, transaction);
        }

        var result = await connection.ExecuteAsync("""
        UPDATE Movie
        SET Slug = @Slug, Title = @Title, YearOfRelease = @YearOfRelease
        WHERE Id = @Id
        """, movie, transaction);

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync("""
        DELETE
        FROM Genre
        where MovieId = @MovieId
        """, new { MovieId = id }, transaction);

        var result = await connection.ExecuteAsync("""
        DELETE
        FROM Movie
        WHERE Id = @Id
        """, new { Id = id }, transaction);

        transaction.Commit();

        return result > 0;
    }

    public async Task<bool> ExistsByIdAsync(Guid id)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync();

        return await connection.ExecuteScalarAsync<bool>("""
        SELECT COUNT(1)
        FROM Movie
        WHERE Id = @Id
        """, new { id });
    }
}