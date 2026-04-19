namespace CaseManager.Services;

public class SystemClock : IClock
{
    public DateTime Now()
    {
        return DateTime.Now;
    }

    public DateTime UtcNow()
    {
        return DateTime.UtcNow;
    }

    public DateTimeOffset NowOffset()
    {
        return DateTimeOffset.Now;
    }
}