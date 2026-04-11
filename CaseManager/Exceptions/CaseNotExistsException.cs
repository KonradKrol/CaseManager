namespace CaseManager.Exceptions;

public class CaseNotExistsException(Guid caseId) : Exception
{
    public Guid CaseId { get; } = caseId;
}