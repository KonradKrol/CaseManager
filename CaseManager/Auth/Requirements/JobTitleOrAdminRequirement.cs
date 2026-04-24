using CaseManager.DomainModels;
using Microsoft.AspNetCore.Authorization;

namespace CaseManager.Auth.Requirements;

public class JobTitleOrAdminRequirement : IAuthorizationRequirement
{
    public required JobTitle JobTitle { get; init; }
}

public class JobTitleOrAdminHandler : AuthorizationHandler<JobTitleOrAdminRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
        JobTitleOrAdminRequirement requirement)
    {
        var jobTitle = context.User.FindFirst("jobTitle")?.Value;
        var jobMatches = jobTitle == requirement.JobTitle.ToString();
        var isAdmin = context.User.FindFirst("role")?.Value is "Admin";

        if (isAdmin || jobMatches)
        {
            context.Succeed(requirement);
        }
        else
        {
            // context.Fail(); TODO: Use it or not?
        }

        return Task.CompletedTask;
    }
}