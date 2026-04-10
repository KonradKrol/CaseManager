namespace CaseManager.Models;

public abstract class User
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public string Surname { get; init; }
    public string Email { get; init; }
    
    public string FullName => $"{Name} {Surname}";

    public abstract string Role { get; }
}

public class Admin : User
{
    public override string Role => "Admin";
}

public class Worker : User
{
    public override string Role => "Worker";
}