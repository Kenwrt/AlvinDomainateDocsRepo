
using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsData.FluentValidation;

public class DocumentLibraryValidator : AbstractValidator<DocumentLibrary>
{
    public DocumentLibraryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.LoanApplicationId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

       
    }
}

public class DocumentSetValidator : AbstractValidator<DocumentSet>
{
    public DocumentSetValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleForEach(x => x.Documents)
            .SetValidator(new DocumentValidator());
    }
}

public class DocumentValidator : AbstractValidator<Document>
{
    public DocumentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();

        RuleFor(x => x.State)
            .IsInEnum();
    }
}
