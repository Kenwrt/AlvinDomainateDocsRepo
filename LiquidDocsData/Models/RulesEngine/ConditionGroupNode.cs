using MongoDB.Bson.Serialization.Attributes;

namespace LiquidDocsData.Models.RulesEngine;

[BsonDiscriminator("group")]
public sealed class ConditionGroupNode : ConditionNode
{
    public ConditionGroup Group { get; set; } = new();
}