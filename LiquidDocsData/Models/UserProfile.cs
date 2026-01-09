using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    [Required]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("UserId")]
    public Guid UserId { get; set; }

    [BsonElement("UserName")]
    public string? UserName { get; set; }

    public string Password { get; set; }

    public string ConfirmedPassword { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.Roles UserRole { get; set; }

    public UserDefaultProfile UserDefaultProfile { get; set; } = new();


    public List<Guid> LoanAgreementGuids { get; set; } = new();
       
    public CreditCardSubscription? CreditCardSubscriotion { get; set; } = new();

    public List<ChargingAuditTrail>? CharingAuditTrails { get; set; } = new();


}