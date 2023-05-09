using FluentValidation;
using Movies.Application.Models;
using Movies.Application.Repositories;

namespace Movies.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IValidator<Movie> _movieValidator;

    public MovieService(IMovieRepository movieRepository, IValidator<Movie> movieValidator)
    {
        _movieRepository = movieRepository;
        _movieValidator = movieValidator;
    }

    public async Task<bool> CreateAsync(Movie movie, CancellationToken cancellationToken)
    {
        await _movieValidator.ValidateAndThrowAsync(movie);
        return await _movieRepository.CreateAsync(movie);
    }

    public Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _movieRepository.GetByIdAsync(id);
    }

    public Task<Movie?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        return _movieRepository.GetBySlugAsync(slug);
    }

    public Task<IEnumerable<Movie>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _movieRepository.GetAllAsync();
    }

    public async Task<Movie?> UpdateAsync(Movie movie, CancellationToken cancellationToken)
    {
        await _movieValidator.ValidateAndThrowAsync(movie);
        var movieExists = await _movieRepository.ExistsByIdAsync(movie.Id);
        if (!movieExists)
        {
            return null;
        }

        await _movieRepository.UpdateAsync(movie);
        return movie;
    }

    public Task<bool> DeleteByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _movieRepository.DeleteByIdAsync(id);
    }
}