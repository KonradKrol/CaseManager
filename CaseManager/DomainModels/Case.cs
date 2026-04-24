using CaseManager.Exceptions;

namespace CaseManager.DomainModels;

public class Case
{
    public Case(Guid id, Guid authorId, string title, string description, List<Guid> assignedTo, CaseStatus status,
        DateTime createdAt,
        DateTime? closedAt = null)
    {
        if (!(status is CaseStatus.Closed ^ closedAt is null))
        {
            throw new DomainEntityCreationException("ClosedAt must be provided when Case is Closed and only then.");
        }

        if (closedAt <= CreatedAt)
        {
            throw new DomainEntityCreationException("ClosedAt cannot be earlier than or equal CreatedAt.");
        }

        if (id.Equals(authorId))
        {
            throw new DomainEntityCreationException("Id and AuthorId must be different.");
        }

        Id = id;
        AuthorId = authorId;
        Title = title;
        Description = description;
        AssignedTo = assignedTo;
        Status = status;
        CreatedAt = createdAt;
        ClosedAt = closedAt;
    }

    public Guid Id { get; }
    public Guid AuthorId { get; }
    public List<Guid> AssignedTo { get; }
    public string Title { get; }
    public string Description { get; }
    public CaseStatus Status { get; }
    public DateTime CreatedAt { get; }
    public DateTime? ClosedAt { get; }

    public bool IsAssignedTo(Guid userId) => AssignedTo.Contains(userId);
    public bool IsAssignedOnlyTo(Guid userId) => AssignedTo.Count == 1 && IsAssignedTo(userId);
    public bool HasSomeoneAssigned() => AssignedTo.Count != 0;
}

public enum CaseStatus
{
    Open,
    InProgress,
    Closed
}