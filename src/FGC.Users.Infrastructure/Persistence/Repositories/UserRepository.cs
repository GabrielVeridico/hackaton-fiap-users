using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FGC.Users.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<User?> FindByEmailAsync(string email)
    {
        return await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> FindByIdAsync(Guid id)
    {
        return await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task SaveNewAsync(User user)
    {
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        await _db.SaveChangesAsync();
    }
}
