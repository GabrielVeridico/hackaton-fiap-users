using HackatonFiap.Users.Application.Commands.RefreshTokenFlow;
using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Commands;

public class RefreshTokenCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refresh = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenService _refreshSvc = Substitute.For<IRefreshTokenService>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _handler = new RefreshTokenCommandHandler(_users, _refresh, _refreshSvc, _jwt);
        _refreshSvc.ComputeHash("raw").Returns("hash");
        _refreshSvc.Generate().Returns(("raw2", "hash2"));
        _refreshSvc.RefreshTokenDays.Returns(7);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns(new AccessToken("jwt", DateTime.UtcNow.AddHours(4)));
    }

    private static User U() => User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "J", "j@x.com", new Password("h"));

    [Fact]
    public async Task Refresh_Valid_RotatesAndReturnsNewPair()
    {
        var user = U();
        var token = RefreshToken.Issue(user.Id, "hash", DateTime.UtcNow.AddDays(7));
        _refresh.FindByHashAsync("hash").Returns(token);
        _users.FindByIdAsync(user.Id).Returns(user);

        var result = await _handler.HandleAsync(new RefreshTokenCommand("raw", "corr"));

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().Be("raw2");
        token.RevokedAtUtc.Should().NotBeNull();
        await _refresh.Received(1).AddAsync(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Refresh_ReusedRevokedToken_RevokesChainAndFails()
    {
        var user = U();
        var revoked = RefreshToken.Issue(user.Id, "hash", DateTime.UtcNow.AddDays(7));
        revoked.Revoke(Guid.NewGuid());
        _refresh.FindByHashAsync("hash").Returns(revoked);

        var result = await _handler.HandleAsync(new RefreshTokenCommand("raw", "corr"));

        result.Error.Code.Should().Be(UserErrors.InvalidRefreshToken.Code);
        await _refresh.Received(1).RevokeAllForUserAsync(user.Id);
    }

    [Fact]
    public async Task Refresh_UnknownToken_Fails()
    {
        _refresh.FindByHashAsync(Arg.Any<string>()).Returns((RefreshToken?)null);
        var result = await _handler.HandleAsync(new RefreshTokenCommand("raw", "corr"));
        result.Error.Code.Should().Be(UserErrors.InvalidRefreshToken.Code);
    }
}
