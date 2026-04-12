using CaseManager.Exceptions;

namespace CaseManager.DomainModels;

public class Comment
{
    public Comment(Guid id, Guid caseId, Guid userId, string message)
    {
        if (message.Length == 0)
        {
            throw new DomainEntityCreationException("Message can't be empty.");
        }
        
        Id = id;
        CaseId = caseId;
        UserId = userId;
        Message = message;
    }

    public Guid Id { get; init; }
    public Guid CaseId { get; init; }
    public Guid UserId { get; init; }
    public string Message { get; init; }
}