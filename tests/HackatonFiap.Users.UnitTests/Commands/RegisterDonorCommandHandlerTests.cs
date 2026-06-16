using HackatonFiap.Users.Application.Commands.RegisterDonor;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Commands;

public class RegisterDonorCommandHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private readonly RegisterDonorCommandHandler _handler;

    public RegisterDonorCommandHandlerTests()
    {
        _handler = new RegisterDonorCommandHandler(_users, _hasher, _audit);
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
    }

    private static RegisterDonorCommand Valid() =>
        new(PersonType.Individual, "52998224725", "João", "joao@x.com", "Senha@123", "corr");

    [Fact]
    public async Task Register_WithValidData_CreatesDonor()
    {
        _users.FindByEmailIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);
        _users.FindByDocumentIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);

        var result = await _handler.HandleAsync(Valid());

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(UserRole.Doador.ToString());
        await _users.Received(1).SaveNewAsync(Arg.Is<User>(u => u.Role == UserRole.Doador));
    }

    [Fact]
    public async Task Register_WithInvalidDocument_Fails()
    {
        var cmd = Valid() with { Document = "111.111.111-11" };
        var result = await _handler.HandleAsync(cmd);
        result.Error.Code.Should().Be(UserErrors.DocumentInvalid.Code);
    }

    [Fact]
    public async Task Register_WithExistingActiveEmail_FailsAlreadyRegistered()
    {
        var existing = User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "X", "joao@x.com", new Password("h"));
        _users.FindByEmailIncludingInactiveAsync("joao@x.com").Returns(existing);

        var result = await _handler.HandleAsync(Valid());
        result.Error.Code.Should().Be(UserErrors.AlreadyRegisteredActive.Code);
    }

    [Fact]
    public async Task Register_WithExistingInactiveEmail_FailsInactiveAccount()
    {
        var existing = User.RegisterDonor(PersonType.Individual, Document.Create("52998224725", PersonType.Individual), "X", "joao@x.com", new Password("h"));
        existing.Deactivate();
        _users.FindByEmailIncludingInactiveAsync("joao@x.com").Returns(existing);

        var result = await _handler.HandleAsync(Valid());
        result.Error.Code.Should().Be(UserErrors.AlreadyRegisteredInactive.Code);
    }
}
