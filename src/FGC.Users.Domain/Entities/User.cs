using FGC.Users.Domain.Enums;
using FGC.Users.Domain.ValueObjects;

namespace FGC.Users.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public Password Password { get; private set; } = null!;
    public UserRole Role { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private User() { }

    public static User Create(string name, string email, Password password, UserRole role = UserRole.User)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            Password = password,
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateProfile(string name, Password? password = null)
    {
        Name = name;
        if (password is not null)
            Password = password;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
