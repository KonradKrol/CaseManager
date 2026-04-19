namespace CaseManager.Auth;

public static class Claims
{
    public static class Roles
    {
        public const string Admin = "admin";
        public const string Regular = "regular";
    }

    public const string Role = "role";

    public const string Name = "name";

    public const string JobTitle = "jobTitle";
    public const string OnboardingStatus = "onboardingStatus";
}