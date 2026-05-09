using CaseManager.DomainModels;
using System.Text.Json;
using CaseManager.Config;
using CaseManager.DomainModels;
using Microsoft.Extensions.Options;

namespace CaseManager.Repository.FileSystem;

public class FileSystemUsers : IUserRepository
{
    private readonly string _filePath;

    private readonly ILogger<FileSystemUsers> _logger;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    public FileSystemUsers(IOptions<StorageOptions> options, ILogger<FileSystemUsers> logger)
    {
        _filePath = options.Value.UsersFilePath;
        _logger = logger;

        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, "[]");
        }
    }

    private async Task<List<UserRecord>> ReadAll()
    {
        await using var stream = File.OpenRead(_filePath);
        try
        {
            var records = await JsonSerializer.DeserializeAsync<List<UserRecord>>(stream)
                   ?? [];

            _logger.LogDebug("Read {RecordsCount} records from file {FilePath}", records.Count, _filePath);
            return records;
        }
        catch (JsonException e)
        {
            return [];
        }
    }

    private async Task WriteAll(List<UserRecord> records)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, records);
        _logger.LogDebug("Written {RecordsCount} records to the file: {FilePath}", records.Count, _filePath);
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        var records = await ReadAll();
        return records.Select(UserMapper.ToDomain);
    }

    public async Task<int> GetCountAsync()
    {
        return (await GetAllUsersAsync())
            .Count();
    }


    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        var users = await ReadAll();
        var userDto = users.SingleOrDefault(u => u.Id == id);

        return userDto is null ? null : UserMapper.ToDomain(userDto);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        var users = await ReadAll();
        var userDto = users.SingleOrDefault(u => u.Email == email);
        return userDto is null ? null : UserMapper.ToDomain(userDto);
    }

    public async Task<bool> UserExistsAsync(Guid id)
    {
        return await GetUserByIdAsync(id) is not null;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await GetUserByEmailAsync(email) is not null;
    }

    public async Task<bool> EveryUserExistsAsync(IEnumerable<Guid> ids)
    {
        return (await GetNotExistingUserIdsAsync(ids)).Count() == 0;
    }

    public Task<bool> GetNotExistingUserIds(IEnumerable<Guid> ids, out IEnumerable<Guid> notExistingIds)
    {
        throw new NotImplementedException();
    }

    public async Task<IEnumerable<Guid>> GetNotExistingUserIdsAsync(IEnumerable<Guid> ids)
    {
        var users = await ReadAll();

        var notExistingIds = ids
            .Where(id => users.All(u => u.Id != id))
            .ToList();

        return notExistingIds;
    }


    public async Task AddUserAsync(User user)
    {
        var records = await ReadAll();
        records.Add(UserMapper.ToRecord(user));
        await WriteAll(records);
    }

    public async Task<bool> DeleteUserAsync(Guid id)
    {
        var records = await ReadAll();
        var indexToRemove = records.FindIndex(record => record.Id == id);
        if (indexToRemove == -1)
        {
            return false;
        }

        records.RemoveAt(indexToRemove);
        await WriteAll(records);
        return true;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        var records = await ReadAll();
        var indexToUpdate = records.FindIndex(record => record.Id == user.Id);
        if (indexToUpdate == -1)
        {
            return false;
        }

        records[indexToUpdate] = UserMapper.ToRecord(user);
        await WriteAll(records);
        return true;
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