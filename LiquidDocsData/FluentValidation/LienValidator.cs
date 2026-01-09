using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsData.FluentValidation;

public class LienValidator : AbstractValidator<Lien>
{
    public LienValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.LienPosition)
            .IsInEnum();
    }
}