using CaseManager.DomainModels;

namespace CaseManager.Exceptions;

public sealed class InvalidCaseStatusException(CaseStatus status, CaseStatus expectedStatus, string? messageForClient)
    : Exception
{
    public CaseStatus Status { get; } = status;
    public CaseStatus ExpectedStatus { get; } = expectedStatus;
    public string? MessageForClient { get; } = messageForClient;
}