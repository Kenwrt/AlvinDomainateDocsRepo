using LiquidDocsData.Enums;
using LiquidDocsData.Models;
using Microsoft.AspNetCore.Identity;

namespace LiquidDocsSite.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }

        public string? DateOfBirth { get; set; }

        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? ZipCode { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public UserEnums.Roles Role { get; set; } = UserEnums.Roles.User;

        //[NotMapped]
        public CreditCardSubscription? CreditCardSubscription { get; set; } = null;
    }
}