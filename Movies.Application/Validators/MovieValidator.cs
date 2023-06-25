using FluentValidation;
using Movies.Application.Models;

namespace Movies.Application.Validators;

public class MovieValidator : AbstractValidator<Movie>
{
    public MovieValidator()
    {
        RuleFor(m => m.Id)
            .NotEmpty();

        RuleFor(m => m.Genres)
            .NotEmpty();

        RuleFor(m => m.Title)
            .NotEmpty();

        RuleFor(m => m.YearOfRelease)
            .LessThanOrEqualTo(DateTime.UtcNow.Year);
    }
}