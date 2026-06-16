using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FGC.Users.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.OwnsOne(u => u.Password, pw =>
        {
            pw.Property(p => p.HashValue)
                .HasColumnName("PasswordHash")
                .IsRequired();
        });

        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.HasQueryFilter(u => u.IsActive);
    }
}
