namespace CaseManager.Models;

public class Comment
{
    public Guid Id { get; init; }
    public Guid CaseId { get; init; }
    public Guid UserId { get; init; }
    public string Message { get; init; }
}