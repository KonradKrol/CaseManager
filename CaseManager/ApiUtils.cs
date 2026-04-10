using FluentValidation;
using FluentValidation.Results;

namespace CaseManager;

public static class ApiUtils
{
    extension<T>(IValidator<T> validator)
    {
        // public void ValidateAndThrow(T validatedObject)
        // {
        //     var failures = validator.ValidateFailures(validatedObject);
        //     if (failures is null) return;
        //     throw new ValidationException(failures);
        // }
        
        public List<ValidationFailure>? ValidateFailures(T validatedObject)
        {
            var validationResult = validator.Validate(validatedObject);
            return validationResult.IsValid ? null : validationResult.Errors;
        }
    }
}