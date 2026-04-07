namespace CaseManager.Models;

public class Case
{
    public Guid Id { get; init; }
    public List<Guid> AssignedTo { get; init; }
    public string Title { get; init; }
    public string Description { get; init; }
    public CaseStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
    
    public bool IsAssignedTo(Guid userId) => AssignedTo.Contains(userId);
    public bool IsAssignedOnlyTo(Guid userId) => AssignedTo.Count == 1 && IsAssignedTo(userId);
    public bool HasAssignedUser() => AssignedTo.Count != 0;
}

public enum CaseStatus
{
    Open,
    InProgress,
    Closed
}