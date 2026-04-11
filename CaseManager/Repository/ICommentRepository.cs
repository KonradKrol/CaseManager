using CaseManager.Models;

namespace CaseManager.Repository;

public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetAllCommentsByCaseId(Guid caseId);
    Task<Comment?> GetCommentById(Guid id);
    Task AddComment(Comment comment);
}