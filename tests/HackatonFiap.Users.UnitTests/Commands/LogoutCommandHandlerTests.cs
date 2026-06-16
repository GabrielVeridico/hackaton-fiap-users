using HackatonFiap.Users.Application.Commands.Logout;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Commands;

public class LogoutCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refresh = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenService _refreshSvc = Substitute.For<IRefreshTokenService>();
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _handler = new LogoutCommandHandler(_refresh, _refreshSvc);
        _refreshSvc.ComputeHash("raw").Returns("hash");
    }

    [Fact]
    public async Task Logout_RevokesActiveToken()
    {
        var token = RefreshToken.Issue(Guid.NewGuid(), "hash", DateTime.UtcNow.AddDays(7));
        _refresh.FindByHashAsync("hash").Returns(token);

        var result = await _handler.HandleAsync(new LogoutCommand("raw", "corr"));

        result.IsSuccess.Should().BeTrue();
        token.RevokedAtUtc.Should().NotBeNull();
        await _refresh.Received(1).SaveChangesAsync();
    }

    [Fact]
    public async Task Logout_UnknownToken_IsIdempotentSuccess()
    {
        _refresh.FindByHashAsync(Arg.Any<string>()).Returns((RefreshToken?)null);

        var result = await _handler.HandleAsync(new LogoutCommand("raw", "corr"));

        result.IsSuccess.Should().BeTrue();
        await _refresh.DidNotReceive().SaveChangesAsync();
    }
}
