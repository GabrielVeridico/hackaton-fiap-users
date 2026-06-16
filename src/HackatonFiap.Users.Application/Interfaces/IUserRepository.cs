using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByIdAsync(Guid id);
    Task<User?> FindByEmailAsync(string email);
    Task SaveNewAsync(User user);
    Task UpdateAsync(User user);
}
