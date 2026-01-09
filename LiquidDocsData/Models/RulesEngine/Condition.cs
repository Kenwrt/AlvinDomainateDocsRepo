using static LiquidDocsData.Models.RulesEngine.Enums.RulesEnums;

namespace LiquidDocsData.Models.RulesEngine;

public sealed class Condition
{
    public string FieldKey { get; set; } = ""; // "@State_Generated"

    public ConditionalOperator Operator { get; set; } = ConditionalOperator.Equals;

    // Null/empty allowed depending on operator
    public List<string>? Values { get; set; }

    public FieldValueType? FieldTypeHint { get; set; }
}