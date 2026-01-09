using static LiquidDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace LiquidDocsData.Models.RulesEngine;

public sealed class ConditionTerm
{
    public ConditionNode Node { get; set; } = new ConditionLeaf();
    public LogicalOperator JoinToNext { get; set; } = LogicalOperator.And;
}