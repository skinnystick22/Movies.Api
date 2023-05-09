using Dapper;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        await connection.ExecuteAsync("""
        CREATE TABLE Movie
        (
            Id            UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
            Slug          nvarchar(512) not null,
            Title         nvarchar(512) not null,
            YearOfRelease int           not null
        )
        """);

        await connection.ExecuteAsync("""
        CREATE NONCLUSTERED INDEX IX_Movie_Slug ON Movie (Slug)
        ALTER TABLE Movie Add CONSTRAINT UQ_Slug UNIQUE (Slug)
        """);

        await connection.ExecuteAsync("""
        CREATE TABLE Genre
        (
            MovieId UNIQUEIDENTIFIER NOT NULL FOREIGN KEY REFERENCES Movie (Id),
            Name    NVARCHAR(255)    NOT NULL
        )
        """);

        await connection.ExecuteAsync("""
        CREATE NONCLUSTERED INDEX IX_Genre_MovieId_Include_Name ON Genre (MovieId) INCLUDE (Name)
        """);
    }
}