using HackatonFiap.Users.Application.Commands.AuthenticateUser;
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

public class AuthenticateUserCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenGenerator _jwt = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenRepository _refresh = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenService _refreshSvc = Substitute.For<IRefreshTokenService>();
    private readonly AuthenticateUserCommandHandler _handler;

    public AuthenticateUserCommandHandlerTests()
    {
        _handler = new AuthenticateUserCommandHandler(_users, _hasher, _jwt, _refresh, _refreshSvc);
        _jwt.GenerateAccessToken(Arg.Any<User>()).Returns(new AccessToken("jwt", DateTime.UtcNow.AddHours(4)));
        _refreshSvc.Generate().Returns(("raw", "hash"));
        _refreshSvc.RefreshTokenDays.Returns(7);
    }

    private static User ActiveUser() =>
        User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "J", "j@x.com", new Password("hashed"));

    [Fact]
    public async Task Login_Valid_ReturnsTokensAndPersistsRefresh()
    {
        var user = ActiveUser();
        _users.FindByEmailAsync("j@x.com").Returns(user);
        _hasher.Verify("Senha@123", "hashed").Returns(true);

        var result = await _handler.HandleAsync(new AuthenticateUserCommand("j@x.com", "Senha@123", "corr"));

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("jwt");
        result.Value.RefreshToken.Should().Be("raw");
        await _refresh.Received(1).AddAsync(Arg.Any<RefreshToken>());
    }

    [Fact]
    public async Task Login_WrongPassword_Fails()
    {
        var user = ActiveUser();
        _users.FindByEmailAsync("j@x.com").Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var result = await _handler.HandleAsync(new AuthenticateUserCommand("j@x.com", "x", "corr"));
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task Login_InactiveUser_Fails()
    {
        var user = ActiveUser();
        user.Deactivate();
        _users.FindByEmailAsync("j@x.com").Returns(user);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await _handler.HandleAsync(new AuthenticateUserCommand("j@x.com", "x", "corr"));
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }
}
