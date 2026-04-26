using CaseManager.DomainModels;
using CaseManager.Repository;
using Microsoft.AspNetCore.Authorization;

namespace CaseManager.Auth.Requirements;

public class CaseAuthorOrActiveAdminRequirement : IAuthorizationRequirement
{
}

public class CaseAuthorOrActiveAdminHandler(
    ILogger<CaseAuthorOrActiveAdminHandler> logger,
    IUserRepository userRepository,
    ICaseRepository caseRepository)
    : AuthorizationHandler<CaseAuthorOrActiveAdminRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context,
        CaseAuthorOrActiveAdminRequirement requirement)
    {
        var resource = context.Resource;
        var sub = context.User.FindFirst("sub")?.Value;

        if (sub is null)
        {
            context.Fail();
            return;
        }

        var userId = Guid.Parse(sub);
        var user = await userRepository.GetUserByIdAsync(userId);

        switch (user)
        {
            case null:
                context.Fail();
                return;
            case { Role: UserRole.Admin, OnboardingStatus: OnboardingStatus.Done }:
                context.Succeed(requirement);
                return;
        }

        var caseAuthorId = resource switch
        {
            HttpContext httpContext => await RetrieveAuthorIdFromHttpContext(httpContext),
            Case @case => @case.AuthorId,
            _ => null,
        };

        var userIsCaseAuthor = caseAuthorId == userId;

        if (!userIsCaseAuthor)
        {
            logger.LogWarning("Have not found any AuthorId for the case.");

            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }

    private async Task<Guid?> RetrieveAuthorIdFromHttpContext(HttpContext context)
    {
        var caseIdString = (string?)context.GetRouteValue("id");
        if (caseIdString is null) return null;
        var parsed = Guid.TryParse(caseIdString, out var caseId);
        if (!parsed) return null;

        var @case = await caseRepository.GetCaseById(caseId);

        return @case?.AuthorId;
    }
}