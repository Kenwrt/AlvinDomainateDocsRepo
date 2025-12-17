public class UserSession
{
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? UserRole { get; set; }
    public string? UserPolicy { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset ExpUtc { get; set; }
}