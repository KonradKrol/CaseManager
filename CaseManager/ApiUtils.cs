using CaseManager.Exceptions;
using FluentValidation;
using FluentValidation.Results;

namespace CaseManager;

public static class ApiUtils
{
    extension<T>(IValidator<T> validator)
    {
        public void ValidateOutputDtosAndThrowFirstError(IEnumerable<T> validatedObjects)
        {
            foreach (var validatedObject in validatedObjects)
            {
                var failures = validator.ValidateFailures(validatedObject);
                if (failures is null) continue;
                var validationException = new ValidationException(failures);
                throw new InternalOutputValidationError(typeof(T), failures, validationException.Message);
            }
        }

        public void ValidateOutputDtoAndThrow(T validatedObjects)
        {
            var failures = validator.ValidateFailures(validatedObjects);
            if (failures is null) return;
            var validationException = new ValidationException(failures);
            throw new InternalOutputValidationError(typeof(T), failures, validationException.Message);
        }

        public List<ValidationFailure>? ValidateFailures(T validatedObject)
        {
            var validationResult = validator.Validate(validatedObject);
            return validationResult.IsValid ? null : validationResult.Errors;
        }
    }
}