using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MicCheck.Api.Identities;

namespace MicCheck.Api.Segments;

public class SegmentEvaluator
{
    public bool Evaluate(Segment segment, IReadOnlyList<IdentityTrait> traits, string identifier = "")
    {
        var topLevelRules = segment.Rules.Where(r => r.ParentRuleId == null).ToList();
        return topLevelRules.All(rule => EvaluateRule(rule, traits, identifier, segment.Id));
    }

    private bool EvaluateRule(
        SegmentRule rule,
        IReadOnlyList<IdentityTrait> traits,
        string identifier,
        int segmentId)
    {
        var conditionResults = rule.Conditions
            .Select(c => EvaluateCondition(c, traits, identifier, segmentId))
            .ToList();

        bool conditionsPass = rule.Type switch
        {
            SegmentRuleType.All => conditionResults.Count == 0 || conditionResults.All(r => r),
            SegmentRuleType.Any => conditionResults.Count > 0 && conditionResults.Any(r => r),
            SegmentRuleType.None => conditionResults.Count == 0 || conditionResults.All(r => !r),
            _ => false
        };

        if (!conditionsPass)
            return false;

        return rule.ChildRules.All(child => EvaluateRule(child, traits, identifier, segmentId));
    }

    private bool EvaluateCondition(
        SegmentCondition condition,
        IReadOnlyList<IdentityTrait> traits,
        string identifier,
        int segmentId)
    {
        var trait = traits.FirstOrDefault(t => t.Key == condition.Property);

        return condition.Operator switch
        {
            SegmentConditionOperator.IsSet => trait is not null,
            SegmentConditionOperator.IsNotSet => trait is null,
            SegmentConditionOperator.PercentageSplit => EvaluatePercentageSplit(condition.Value, identifier, segmentId),
            _ => trait is not null && EvaluateTraitCondition(condition, trait.Value)
        };
    }

    private static bool EvaluateTraitCondition(SegmentCondition condition, string traitValue)
    {
        return condition.Operator switch
        {
            SegmentConditionOperator.Equal => traitValue.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.NotEqual => !traitValue.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.Contains => traitValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.NotContains => !traitValue.Contains(condition.Value, StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.Regex => Regex.IsMatch(traitValue, condition.Value),
            SegmentConditionOperator.GreaterThan => CompareNumeric(traitValue, condition.Value) > 0,
            SegmentConditionOperator.GreaterThanOrEqual => CompareNumeric(traitValue, condition.Value) >= 0,
            SegmentConditionOperator.LessThan => CompareNumeric(traitValue, condition.Value) < 0,
            SegmentConditionOperator.LessThanOrEqual => CompareNumeric(traitValue, condition.Value) <= 0,
            SegmentConditionOperator.In => condition.Value.Split(',').Any(v => v.Trim().Equals(traitValue, StringComparison.OrdinalIgnoreCase)),
            SegmentConditionOperator.NotIn => !condition.Value.Split(',').Any(v => v.Trim().Equals(traitValue, StringComparison.OrdinalIgnoreCase)),
            SegmentConditionOperator.IsTrue => traitValue.Equals("true", StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.IsFalse => traitValue.Equals("false", StringComparison.OrdinalIgnoreCase),
            SegmentConditionOperator.ModuloValueDivisorRemainder => EvaluateModulo(traitValue, condition.Value),
            _ => false
        };
    }

    private static int CompareNumeric(string traitValue, string conditionValue)
    {
        if (!decimal.TryParse(traitValue, out var trait) ||
            !decimal.TryParse(conditionValue, out var condition))
            return 0;
        return trait.CompareTo(condition);
    }

    private static bool EvaluateModulo(string traitValue, string conditionValue)
    {
        var parts = conditionValue.Split('|');
        if (parts.Length != 2)
            return false;
        if (!long.TryParse(traitValue, out var value) ||
            !long.TryParse(parts[0], out var divisor) ||
            !long.TryParse(parts[1], out var remainder))
            return false;
        if (divisor == 0)
            return false;
        return value % divisor == remainder;
    }

    private static bool EvaluatePercentageSplit(string conditionValue, string identifier, int segmentId)
    {
        if (!decimal.TryParse(conditionValue, out var percentage))
            return false;

        var input = $"{identifier}{segmentId}";
        var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        var bucket = (hashValue % 9999m) / 99.99m;

        return bucket < percentage;
    }
}
