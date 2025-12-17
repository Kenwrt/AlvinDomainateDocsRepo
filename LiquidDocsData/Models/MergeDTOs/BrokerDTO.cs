using MongoDB.Bson.Serialization.Attributes;

namespace LiquidDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class BrokerDTO
{
    public List<Broker> BrokerList { get; set; } = new();

    public string EntityDescriptors { get; set; } = string.Empty;

    public string EntitySignatureLines { get; set; } = string.Empty;
}