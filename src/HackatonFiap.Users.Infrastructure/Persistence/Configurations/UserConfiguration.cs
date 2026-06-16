using HackatonFiap.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HackatonFiap.Users.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name).IsRequired().HasMaxLength(200);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PersonType).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(50);
        builder.Property(u => u.IsActive).HasDefaultValue(true);
        builder.Property(u => u.IsOwner).HasDefaultValue(false);

        builder.OwnsOne(u => u.Document, doc =>
        {
            doc.Property(d => d.Value).HasColumnName("Document").IsRequired().HasMaxLength(14);
            doc.HasIndex(d => d.Value).IsUnique();
        });

        builder.OwnsOne(u => u.Password, pw =>
        {
            pw.Property(p => p.HashValue).HasColumnName("PasswordHash").IsRequired();
        });

        builder.HasQueryFilter(u => u.IsActive);
    }
}
