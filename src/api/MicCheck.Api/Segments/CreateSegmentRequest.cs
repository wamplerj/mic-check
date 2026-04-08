using FluentValidation;

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

public class CreateSegmentRequestValidator : AbstractValidator<CreateSegmentRequest>
{
    public CreateSegmentRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Rules).NotNull();
        RuleForEach(x => x.Rules).ChildRules(rule =>
        {
            rule.RuleFor(r => r.Type)
                .Must(t => Enum.TryParse<SegmentRuleType>(t, true, out _))
                .WithMessage("Rule type must be 'All', 'Any', or 'None'.");
            rule.RuleForEach(r => r.Conditions).ChildRules(cond =>
            {
                cond.RuleFor(c => c.Property).NotEmpty();
                cond.RuleFor(c => c.Operator)
                    .Must(o => Enum.TryParse<SegmentConditionOperator>(o, true, out _))
                    .WithMessage("Invalid operator.");
            });
        });
    }
}
