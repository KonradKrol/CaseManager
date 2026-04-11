using CaseManager.Models;

namespace CaseManager.Repository.InMemory;

public class InMemoryCases : ICaseRepository
{
    private readonly List<Case> _cases =
    [
        new Case
        {
            Id = Guid.NewGuid(),
            Title = "Kupcie drukarkę",
            Description = "Drukarka jest nam potrzebna. Najlepiej HP!",
            AssignedTo = [],
            Status = CaseStatus.InProgress,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(4),
            ClosedAt = null,
        },
        new Case
        {
            Id = Guid.NewGuid(),
            Title = "Wywóz śmieci",
            Description = "Wywóz śmieci wywóz smieci!",
            AssignedTo =
            [
                Guid.NewGuid(),
                Guid.NewGuid(),
            ],
            Status = CaseStatus.Closed,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(16),
            ClosedAt = DateTime.Now - TimeSpan.FromHours(12),
        },
        new Case
        {
            Id = Guid.NewGuid(),
            Title = "Problem z pracownikiem...",
            Description = "Pan Mariusz Kowalski dopuścił się karygodnego czynu, a mianowicie [...]",
            AssignedTo =
            [
                Guid.NewGuid()
            ],
            Status = CaseStatus.Open,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(1),
            ClosedAt = null,
        },
        new Case
        {
            Id = Guid.NewGuid(),
            Title = "Cip, cip, kurka. Ku-ku-ryku!",
            Description = "Kurka, kurka, kurka.",
            AssignedTo = [],
            Status = CaseStatus.InProgress,
            CreatedAt = DateTime.Now - TimeSpan.FromDays(90),
            // ClosedAt = DateTime.Now - TimeSpan.FromSeconds(13), // TODO: dla zmyłki. Docelowo, powinien być błąd walidacji w modelu domenowym!
        },
        new Case
        {
            Id = Guid.NewGuid(),
            Title = "Czy w firmie można podkradać kawę?",
            Description = "Zapytanie kieruję do Najwyższego Kierownictwa.",
            AssignedTo =
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
            ],
            Status = CaseStatus.Open,
            CreatedAt = DateTime.Now - TimeSpan.FromSeconds(54),
            ClosedAt = null,
        },
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