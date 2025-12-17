using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class LoanAgreementDTO
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string? ReferenceName { get; set; }

    public string LoanNumber { get; set; }

    public DocumentSet DocumentSet { get; set; }

    public decimal PrincipalAmount { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Loan.Types LoanType { get; set; } = LiquidDocsData.Enums.Loan.Types.ConstructionOrRehab;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.Schedules RepaymentSchedule { get; set; } = LiquidDocsData.Enums.Payment.Schedules.Monthly;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateTypes RateType { get; set; } = LiquidDocsData.Enums.Payment.RateTypes.Fixed;

    public decimal InterestRate { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateIndexes RateIndex { get; set; } = LiquidDocsData.Enums.Payment.RateIndexes.PRIME;

    public bool IsPrepaymentPenalty { get; set; } = false;

    public decimal PrepaymentFee { get; set; }

    public int TermInMonths { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.PrepaymentPremiums PrepaymentPremium { get; set; } = LiquidDocsData.Enums.Payment.PrepaymentPremiums.PenaltyInMonths;

    public int ReserveInMonthsToCalculate { get; set; }

    public decimal ReserveSpecificAmount { get; set; }

    public DateTime? OriginationDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public bool IsTaxInsuranceOtherImpounds { get; set; } = false;

    public bool IsBorrowerResponsibleForServicingFees { get; set; } = false;

    public decimal ServicingFeeAmount { get; set; }

    public bool IsExitFeeIncluded { get; set; } = false;

    public bool IsACHDelivery { get; set; } = false;

    public bool IsRemoveACHDFormFromDocSet { get; set; } = false;

    public decimal ExitFeeAmount { get; set; }

    public bool IsConditionalRightToExtend { get; set; } = false;

    public int NumberOfExtensions { get; set; }

    public int NumberOfMonthsForEachExtension { get; set; }

    public string LoanPreparerName { get; set; }

    public string LoanPreparerStreetAddress { get; set; }

    public string LoanPreparerCity { get; set; }

    public string LoanPreparerState { get; set; }

    public string LoanPreparerZipCode { get; set; }

    public string LoanPreparerCounty { get; set; }

    public string LoanPreparerEmailAddress { get; set; }

    public bool IsW9TObeIncludedInDocSet { get; set; } = false;

    public bool IsLoanIntendedForSale { get; set; } = false;

    public string LoanSalesInformation { get; set; }

    public string LoanPreparerPhoneNumber { get; set; }

    public string LoanPurchaserName { get; set; }

    public string LoanPurchaserStreetAddress { get; set; }

    public string LoanPurchaserCity { get; set; }

    public string LoanPurchaserState { get; set; }

    public string LoanPurchaserZipCode { get; set; }

    public string LoanPurchaserCounty { get; set; }

    public string LoanPurchaserEmailAddress { get; set; }

    public string LoanPurchaserPhoneNumber { get; set; }

    public string LoanPurchaserAssignees { get; set; }

    public bool IsMERSLanuageToBeInserted { get; set; } = false;

    public bool IsSignAffidavitAkaRequired { get; set; } = false;

    public string ClosingContactName { get; set; }

    public string ClosingContactEmail { get; set; }

    public DateTime? SignedDate { get; set; }

    public BorrowerDTO Borrowers { get; set; } = new();

    public BrokerDTO Brokers { get; set; } = new();

    public GuarantorDTO Guarantors { get; set; } = new();

    public LenderDTO Lenders { get; set; } = new();

    public PropertyRecordDTO Properties { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.PerDiemInterestOptions PerDiemOption { get; set; }

    public List<FeeToBePaid> FeesToBePaid { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Loan.Status Status { get; set; } = Loan.Status.Pending;
}