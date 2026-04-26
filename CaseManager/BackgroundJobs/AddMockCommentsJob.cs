using CaseManager.DomainModels;
using CaseManager.Repository;
using CaseManager.Utils;

namespace CaseManager.BackgroundJobs;

public class AddMockCommentsJob(
    IUserRepository userRepository,
    ICaseRepository caseRepository,
    ICommentRepository commentRepository) : BackgroundService
{
    private const int MockedCommentsCount = 50;

    private readonly List<string> _messages =
    [
        "Wow, niesamowite!",
        "Drogi użytkowniku!\nCo dokładnie sprawia, że poświęcasz aż taką uwagę temu problemowi?",
        "Hops hops",
        "Leci Kamil Stoch !",
        "Wow, niesamowite! Widziałeś to? Takie coś mi się nigdy nie zdarzyło!",
        "Haha, masz rację, rzeczywiście tak jest. No, ciekawe, ciekawe jak to będzie.",
        "Ups, nie pomyślałem o tym :-)",
        "Hej, masz jakieś plany na weekend?",
        "Ciekawe kiedy dadzą mi wypowiedzenie",
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

        var allUsers = await userRepository.GetAllUsersAsync();
        var allCases = await caseRepository.GetFirstNCasesByCreatedAt(0, 200);

        var userIds = new List<Guid>()
        {
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()
        };

        // var userIds = allUsers.Select(user => user.Id).ToHashSet().ToList();
        var caseIds = allCases.Select(@case => @case.Id).ToHashSet().ToList();

        await InitializeMockComments(caseIds, userIds);
    }

    private async Task InitializeMockComments(List<Guid> caseIds, List<Guid> authorIds)
    {
        for (var i = 0; i < MockedCommentsCount; i++)
        {
            var comment = new Comment(id: Guid.NewGuid(), caseId: caseIds.GetRandomElement(),
                userId: authorIds.GetRandomElement(), message: _messages.GetRandomElement());

            await commentRepository.AddComment(comment);
        }
    }
}