namespace CaseManager.Exceptions;

public sealed class TooManyAdminsException(int adminsLimit, DateTime adminCreationTimestamp) : Exception
{
    public int AdminsLimit { get; } = adminsLimit;
    public DateTime AdminCreationTimestamp { get; } = adminCreationTimestamp;
}