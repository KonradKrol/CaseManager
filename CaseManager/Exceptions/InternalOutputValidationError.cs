using FluentValidation.Results;

namespace CaseManager.Exceptions;

/// <summary>
/// This exception is thrown when endpoint validates some DTO that has to be returned, and the validation fails. It's not meant to be shown for our API client (only for developers).
/// </summary>
/// <param name="validatedType"></param>
/// <param name="failures"></param>
public class InternalOutputValidationError(Type validatedType, List<ValidationFailure> failures, string originalValidationMessage) : Exception
{
    public Type ValidatedType { get; } = validatedType;
    public List<ValidationFailure> Failures { get; } = failures;
    public override string Message => originalValidationMessage;
}