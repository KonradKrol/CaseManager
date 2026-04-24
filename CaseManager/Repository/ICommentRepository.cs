using CaseManager.DomainModels;

namespace CaseManager.Repository;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetAllCommentsOf(Guid caseId);
    Task<Comment?> GetFirstCommentOf(Guid caseId);
    Task<Comment?> GetCommentById(Guid id);
    
    
    /// Should throw `InvalidOperationException` if cannot relate the comment to a case
    Task AddComment(Comment comment);
}