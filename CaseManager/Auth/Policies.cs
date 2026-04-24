namespace CaseManager.Auth;

public static class Policies
{
    public const string AuthenticatedOnly = "AuthenticatedOnly";

    public const string CaseAuthorOrActiveAdmin = "CaseAuthorOrActiveAdmin";
    
    public const string ItExpertOrAdmin = "ItExpertOrAdmin";
    
    public const string AdminOnly = "AdminOnly";
    
    public const string OnboardedOnly = "OnboardedOnly";
}