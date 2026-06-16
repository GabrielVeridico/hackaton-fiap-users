using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Infrastructure.Audit;
using Microsoft.EntityFrameworkCore;

namespace HackatonFiap.Users.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<AuditEvent> AuditEvents { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
