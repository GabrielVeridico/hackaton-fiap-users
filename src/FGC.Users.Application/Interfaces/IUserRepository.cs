using FGC.Users.Domain.Entities;

namespace FGC.Users.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id);
    Task<User?> FindByEmailAsync(string email);
    Task SaveNewAsync(User user);
    Task UpdateAsync(User user);
}
