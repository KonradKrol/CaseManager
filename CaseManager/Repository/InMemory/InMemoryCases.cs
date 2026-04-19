using CaseManager.DomainModels;

namespace CaseManager.Repository.InMemory;

public class InMemoryCases : ICaseRepository
{
    private readonly List<Case> _cases =
    [
        new(id: Guid.NewGuid(), title: "Kupcie drukarkę",
            description: "Drukarka jest nam potrzebna. Najlepiej HP!", assignedTo: [], status: CaseStatus.InProgress,
            createdAt: DateTime.Now - TimeSpan.FromDays(4), closedAt: null),
        new(id: Guid.NewGuid(), title: "Wywóz śmieci", description: "Wywóz śmieci wywóz smieci!", assignedTo:
            [
                Guid.NewGuid(),
                Guid.NewGuid(),
            ], status: CaseStatus.Closed, createdAt: DateTime.Now - TimeSpan.FromDays(16),
            closedAt: DateTime.Now - TimeSpan.FromHours(12)),
        new(id: Guid.NewGuid(), title: "Problem z pracownikiem...",
            description: "Pan Mariusz Kowalski dopuścił się karygodnego czynu, a mianowicie [...]", assignedTo:
            [
                Guid.NewGuid()
            ], status: CaseStatus.Open, createdAt: DateTime.Now - TimeSpan.FromDays(1), closedAt: null),
        new(id: Guid.NewGuid(), title: "Cip, cip, kurka. Ku-ku-ryku!", description: "Kurka, kurka, kurka.",
            assignedTo: [], status: CaseStatus.InProgress,
            createdAt: DateTime.Now -
                       TimeSpan.FromDays(
                           90)
        ),
        new(id: Guid.NewGuid(), title: "Czy w firmie można podkradać kawę?",
            description: "Zapytanie kieruję do Najwyższego Kierownictwa.", assignedTo:
            [
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
            ], status: CaseStatus.Open, createdAt: DateTime.Now - TimeSpan.FromSeconds(54), closedAt: null),
    ];

    public Task<IEnumerable<Case>> GetFirstNCasesByCreatedAt(int startingAtIndex, int n)
    {
        SortCasesByCreatedAtDescending();
        return Task.FromResult(_cases.Skip(startingAtIndex).Take(n));
    }

    private void SortCasesByCreatedAtDescending()
    {
        _cases.Sort(((caseA, caseB) => caseB.CreatedAt.CompareTo(caseA.CreatedAt)));
    }

    public Task<Case?> GetCaseById(Guid id)
    {
        return Task.FromResult(_cases.SingleOrDefault(@case => @case.Id == id));
    }

    public async Task<bool> CaseExists(Guid id)
    {
        return await GetCaseById(id) != null;
    }

    public Task AddCase(Case @case)
    {
        _cases.Add(@case);
        return Task.CompletedTask;
    }
}