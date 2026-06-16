using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;

namespace HackatonFiap.Users.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public PersonType PersonType { get; private set; }
    public Document Document { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public Password Password { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsOwner { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private User() { }

    public static User RegisterDonor(PersonType personType, Document document, string name, string email, Password password)
        => Build(personType, document, name, email, password, UserRole.Doador, isOwner: false);

    public static User CreateByAdmin(PersonType personType, Document document, string name, string email, Password password, UserRole role)
        => Build(personType, document, name, email, password, role, isOwner: false);

    public static User CreateOwner(Document document, string name, string email, Password password)
        => Build(PersonType.Individual, document, name, email, password, UserRole.GestorONG, isOwner: true);

    private static User Build(PersonType personType, Document document, string name, string email, Password password, UserRole role, bool isOwner)
    {
        var now = DateTime.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            PersonType = personType,
            Document = document,
            Name = name,
            Email = email,
            Password = password,
            Role = role,
            IsActive = true,
            IsOwner = isOwner,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
    }

    public void UpdateProfile(string name) { Name = name; Touch(); }
    public void ChangePassword(Password password) { Password = password; Touch(); }
    public void ChangeRole(UserRole role) { Role = role; Touch(); }
    public void Deactivate() { IsActive = false; Touch(); }
    public void Reactivate() { IsActive = true; Touch(); }

    private void Touch() => UpdatedAtUtc = DateTime.UtcNow;
}
