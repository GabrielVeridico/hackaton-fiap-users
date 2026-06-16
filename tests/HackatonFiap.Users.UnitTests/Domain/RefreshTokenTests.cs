using HackatonFiap.Users.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void Issue_IsActive()
    {
        var t = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));
        t.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Revoke_SetsRevokedAndInactive()
    {
        var t = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));
        var newId = Guid.NewGuid();
        t.Revoke(newId);
        t.RevokedAtUtc.Should().NotBeNull();
        t.ReplacedByTokenId.Should().Be(newId);
        t.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Expired_IsInactive()
    {
        var t = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddSeconds(-1));
        t.IsActive.Should().BeFalse();
    }
}
