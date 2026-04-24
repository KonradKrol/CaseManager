using CaseManager.DomainModels;

namespace CaseManager.Repository.InMemory;

public class InMemoryComments : ICommentRepository
{
    private readonly Dictionary<Guid, List<Comment>> _commentsByCase = new();
    private readonly Dictionary<Guid, Comment> _commentsById = new();

    public Task<IEnumerable<Comment>> GetAllCommentsOf(Guid caseId)
    {
        return Task.FromResult<IEnumerable<Comment>>(_commentsByCase.TryGetValue(caseId, out var comments)
            ? comments
            : []);
    }

    public Task<Comment?> GetFirstCommentOf(Guid caseId)
    {
        return Task.FromResult(_commentsByCase.TryGetValue(caseId, out var comments)
            ? comments.First()
            : null);
    }

    public Task<Comment?> GetCommentById(Guid id)
    {
        _commentsById.TryGetValue(id, out var comment);
        return Task.FromResult(comment);
    }

    public Task AddComment(Comment comment)
    {
        if (!_commentsByCase.TryGetValue(comment.CaseId, out var comments))
        {
            _commentsByCase[comment.CaseId] = [comment];
        }
        else
        {
            comments.Add(comment);
        }

        _commentsById[comment.Id] = comment;

        return Task.CompletedTask;
    }
}