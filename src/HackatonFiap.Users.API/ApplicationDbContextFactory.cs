using HackatonFiap.Users.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HackatonFiap.Users.API;

// Design-time factory: used by `dotnet ef` so it does NOT execute Program.cs
// (which would run Database.Migrate() and the Owner seed against a real database).
// The connection string is only a placeholder — `migrations add` does not connect.
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=HackatonFiapUsersDb;User Id=sa;Password=Design_time_only_1;TrustServerCertificate=true;")
            .Options;

        return new ApplicationDbContext(options);
    }
}
