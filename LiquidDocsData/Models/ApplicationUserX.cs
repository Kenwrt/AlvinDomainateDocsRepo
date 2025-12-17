using LiquidDocsData.Enums;
using Microsoft.AspNetCore.Identity;

namespace LiquidDocsData.Models;

public class ApplicationUserX : IdentityUser
{
    public string? Name { get; set; }

    public string? DateOfBirth { get; set; }

    public string FullAddress { get; set; }

    public string StreetAddress { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string ZipCode { get; set; }

    public string County { get; set; }

    public string Country { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }

    public string? ProfilePictureUrl { get; set; }

    public UserEnums.Roles Role { get; set; } = UserEnums.Roles.User;

    public CreditCardSubscription CreditCardSubscription { get; set; } = new();
}