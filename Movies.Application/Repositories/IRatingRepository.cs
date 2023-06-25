using Movies.Application.Models;

namespace Movies.Application.Repositories;

public interface IRatingRepository
{
    Task<bool> RateMovieAsync(Guid movieId, byte rating, Guid userId, CancellationToken cancellationToken = default);
    Task<float?> GetRatingAsync(Guid movieId, CancellationToken cancellationToken = default);
    Task<(float? Rating, byte? UserRating)> GetRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}