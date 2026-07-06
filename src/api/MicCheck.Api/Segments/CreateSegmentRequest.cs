using MicCheck.Api.Common.Validation;

namespace MicCheck.Api.Segments;

public record CreateSegmentConditionRequest(
    string Property,
    string Operator,
    string Value);

public record CreateSegmentRuleRequest(
    string Type,
    IReadOnlyList<CreateSegmentConditionRequest> Conditions,
    IReadOnlyList<CreateSegmentRuleRequest>? ChildRules = null);

public record CreateSegmentRequest(
    string Name,
    IReadOnlyList<CreateSegmentRuleRequest> Rules);

public class CreateSegmentRequestValidator : IModelValidator<CreateSegmentRequest>
{
    public ValidationResult Validate(CreateSegmentRequest model)
    {
        var result = new ValidationResult();

        if (string.IsNullOrEmpty(model.Name))
            result.AddError(nameof(model.Name), "'Name' must not be empty.");
        else if (model.Name.Length > 200)
            result.AddError(nameof(model.Name), "'Name' must be 200 characters or fewer.");

        if (model.Rules is null)
        {
            result.AddError(nameof(model.Rules), "'Rules' must not be empty.");
            return result;
        }

        foreach (var rule in model.Rules)
        {
            if (!Enum.TryParse<SegmentRuleType>(rule.Type, true, out _))
                result.AddError(nameof(model.Rules), "Rule type must be 'All', 'Any', or 'None'.");

            foreach (var condition in rule.Conditions)
            {
                if (string.IsNullOrEmpty(condition.Property))
                    result.AddError(nameof(model.Rules), "'Property' must not be empty.");

                if (!Enum.TryParse<SegmentConditionOperator>(condition.Operator, true, out _))
                    result.AddError(nameof(model.Rules), "Invalid operator.");
            }
        }

        return result;
    }
}
