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
                """, new { MovieId = movie.Id, Name = movieGenre }, transaction, cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();

        return result > 0;
    }

    public async Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(new CommandDefinition("""
        SELECT Id, Slug, Title, YearOfRelease
        FROM Movie
        WHERE Id = @Id
        """, new { Id = id }, cancellationToken: cancellationToken));

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

    public async Task<Movie?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var movie = await connection.QuerySingleOrDefaultAsync<Movie>(new CommandDefinition("""
        SELECT Id, Slug, Title, YearOfRelease
        FROM Movie
        WHERE Slug = @Slug
        """, new { Slug = slug }, cancellationToken: cancellationToken));

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

    public async Task<IEnumerable<Movie>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

        var result = await connection.QueryAsync(new CommandDefinition("""
        SELECT M.Id, M.Title, M.YearOfRelease, STRING_AGG(G.Name, ', ') AS Genres
        FROM Movie M
            LEFT JOIN Genre G on M.Id = G.MovieId
        GROUP BY M.Id, M.Title, M.YearOfRelease
        """, cancellationToken: cancellationToken));

        return result.Select(x => new Movie
        {
            Id = x.Id,
            Title = x.Title,
            YearOfRelease = x.YearOfRelease,
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
            await connection.ExecuteAsync(new  CommandDefinition("""
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