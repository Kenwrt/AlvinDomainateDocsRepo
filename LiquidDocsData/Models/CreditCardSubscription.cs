namespace LiquidDocsData.Models;

public class CreditCardSubscription
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    public string? PlanName { get; set; }
    public decimal? MonthlyCost { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive => !EndDate.HasValue || EndDate.Value > DateTime.UtcNow;

    public CreditCard CreditCard { get; set; } = new();
}