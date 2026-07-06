namespace MicCheck.Api.Common.Validation;

public interface IModelValidator
{
    ValidationResult Validate(object model);
}

public interface IModelValidator<in T> : IModelValidator
{
    ValidationResult Validate(T model);

    ValidationResult IModelValidator.Validate(object model) => Validate((T)model);
}
