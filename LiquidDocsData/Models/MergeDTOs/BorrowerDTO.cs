using MongoDB.Bson.Serialization.Attributes;

namespace LiquidDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class BorrowerDTO
{
    public List<Borrower> BorrowerList { get; set; } = new();

    public string EntityDescriptors { get; set; } = string.Empty;

    public string EntitySignatureLines { get; set; } = string.Empty;
}