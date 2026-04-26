namespace CaseManager.Utils;

public static class EnumUtils
{
    public static bool ContainsValue<T>(string value)
        where T : struct, Enum
    {
        return Enum.TryParse<T>(value, false, out var parsed) &&
               Enum.IsDefined(parsed);
    }
}