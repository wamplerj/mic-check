namespace MicCheck.Api.Common.Validation;

public record ValidationError(string PropertyName, string Message);

public class ValidationResult
{
    private readonly List<ValidationError> _errors = [];

    public IReadOnlyList<ValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;
    public bool IsInvalid => _errors.Count > 0;

    public void AddError(string propertyName, string message) => _errors.Add(new ValidationError(propertyName, message));
}
