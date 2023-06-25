namespace Movies.Contracts.Requests;

public class RateMovieRequest
{
    public required byte Rating { get; init; }
}