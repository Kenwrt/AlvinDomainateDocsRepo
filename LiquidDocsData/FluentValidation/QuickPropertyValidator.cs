
using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsData.FluentValidation;

public class QuickPropertyValidator : AbstractValidator<PropertyRecord>
{
    public QuickPropertyValidator()
    {
        RuleFor(x => x.FullAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);
    }
}