using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]

public class UserDefaultProfile
{
    [Key]
    [Required]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [BsonElement("UserId")]
    public Guid UserId { get; set; }


    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.UserTypes UserType { get; set; } = UserEnums.UserTypes.Lender;


    public string EmailDeliveryAddress { get; set; }
      
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateTypes RateType { get; set; } = LiquidDocsData.Enums.Payment.RateTypes.Fixed;

    public LoanType LoanType { get; set; } = null;

    public List<Guid> AvailableDocumentLibraryGuids { get; set; } = new();

    public Guid DefaultDocumentLibraryGuid { get; set; } = Guid.Parse("533fb231-20f3-4819-8d83-64ede387bd02");

    public decimal PrincipalAmount { get; set; } = 0;

    public decimal InterestRate { get; set; } = 0;

    public decimal MaxInterestAllowed { get; set; } = 0;

    public int TermInMonths { get; set; } = 0;

    public decimal InitialMargin { get; set; } = 0;

    public VariableInterestProperties VariableInterestProperties { get; set; } = new();

    public BalloonPayments BalloonPayments { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.AmortizationTypes AmorizationType { get; set; } = Payment.AmortizationTypes.InterestOnly;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.Schedules RepaymentSchedule { get; set; } = LiquidDocsData.Enums.Payment.Schedules.Monthly;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateIndexes RateIndex { get; set; } = LiquidDocsData.Enums.Payment.RateIndexes.PRIME;
}