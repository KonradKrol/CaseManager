using CaseManager.DomainModels;
using System.Text.Json;
using CaseManager.Config;
using CaseManager.DomainModels;
using Microsoft.Extensions.Options;

namespace CaseManager.Repository.FileSystem;

public class FileSystemUsers : IUserRepository
{
    private readonly string _filePath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public FileSystemUsers(IOptions<StorageOptions> options)
    {
        _filePath = options.Value.UsersFilePath;

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<UserRecord>> ReadAll()
    {
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<UserRecord>>(stream)
               ?? new List<UserRecord>();
    }

    private async Task WriteAll(List<UserRecord> records)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, records);
    }

    public async Task<IEnumerable<User>> GetAllUsers()
    {
        var records = await ReadAll();
        return records.Select(UserMapper.ToDomain);
    }


    public async Task<User?> GetUserById(Guid id)
    {
        var users = await ReadAll();
        var userDto = users.SingleOrDefault(u => u.Id == id);

        return userDto is null ? null : UserMapper.ToDomain(userDto);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        var users = await ReadAll();
        var userDto = users.SingleOrDefault(u => u.Email == email);
        return userDto is null ? null : UserMapper.ToDomain(userDto);
    }

    public async Task<bool> UserExists(Guid id)
    {
        return await GetUserById(id) is not null;
    }

    public async Task<bool> UserExists(string email)
    {
        return await GetUserByEmail(email) is not null;
    }

    public async Task<bool> EveryUserExists(IEnumerable<Guid> ids)
    {
        return (await GetNotExistingUserIds(ids)).Count() == 0;
    }

    public Task<bool> GetNotExistingUserIds(IEnumerable<Guid> ids, out IEnumerable<Guid> notExistingIds)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Guid>> GetNotExistingUserIds(IEnumerable<Guid> ids)
    {
        var users = await ReadAll();

        var notExistingIds = ids
            .Where(id => users.All(u => u.Id != id))
            .ToList();

        return notExistingIds;
    }


    public async Task AddUser(User user)
    {
        var records = await ReadAll();
        records.Add(UserMapper.ToRecord(user));
        await WriteAll(records);
    }
}

internal class UserRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Surname { get; set; } = null!;
    public string Email { get; set; } = null!;
    public UserRole Role { get; set; }
    public JobTitle JobTitle { get; set; }
    public OnboardingStatus OnboardingStatus { get; set; }
    public string PasswordHash { get; set; } = null!;
}

internal static class UserMapper
{
    public static User ToDomain(UserRecord r) =>
        new(r.Id, r.Name, r.Surname, r.Email, r.Role, r.JobTitle, r.OnboardingStatus, r.PasswordHash);

    public static UserRecord ToRecord(User u) =>
        new()
        {
            Id = u.Id,
            Name = u.Name,
            Surname = u.Surname,
            Email = u.Email,
            Role = u.Role,
            JobTitle = u.JobTitle,
            OnboardingStatus = u.OnboardingStatus,
            PasswordHash = u.PasswordHash
        };
}