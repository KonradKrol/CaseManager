namespace CaseManager.Services;

public interface IClock
{
    public DateTime Now();
    public DateTime UtcNow();
    public DateTimeOffset NowOffset();
}