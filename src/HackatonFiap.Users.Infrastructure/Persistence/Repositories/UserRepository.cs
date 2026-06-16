using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Users.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _db;

    public UserRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    // Auth path: respeita o filtro global (soft-delete). Usuário inativo => null => credenciais inválidas (RN01.8).
    // Buscas que precisam enxergar inativos (unicidade de cadastro/admin) usam métodos dedicados *IncludingInactiveAsync.
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

    // Registration/admin path: bypasses the global query filter so inactive (soft-deleted) users are visible.
    // Never used by the auth (login) path — login uses FindByEmailAsync which respects the filter.
    public Task<User?> FindByEmailIncludingInactiveAsync(string email) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == email);

    public Task<User?> FindByDocumentIncludingInactiveAsync(string documentValue) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Document.Value == documentValue);

    public async Task<IReadOnlyList<User>> ListAsync() =>
        await _db.Users.IgnoreQueryFilters().AsNoTracking().OrderBy(u => u.Name).ToListAsync();

    public Task<User?> FindOwnerAsync() =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.IsOwner);

    public Task<User?> FindByIdIncludingInactiveAsync(Guid id) =>
        _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == id);
}
