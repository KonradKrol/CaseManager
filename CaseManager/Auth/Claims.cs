namespace CaseManager.Auth;

public static class Claims
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Regular = "Regular";
    }

    public const string Role = "role";

    public const string Name = "name";

    public const string JobTitle = "jobTitle";
    
    public static class OnboardingStatuses
    {
        public const string NotStarted = "NotStarted";
        public const string InProgress = "InProgress";
        public const string Done = "Done";

    }
    public const string OnboardingStatus = "onboardingStatus";
}