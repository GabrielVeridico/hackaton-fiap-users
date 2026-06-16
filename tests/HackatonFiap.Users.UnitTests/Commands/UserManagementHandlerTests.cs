using HackatonFiap.Users.Application.Commands.ChangeUserRole;
using HackatonFiap.Users.Application.Commands.CreateUser;
using HackatonFiap.Users.Application.Commands.DeactivateUser;
using HackatonFiap.Users.Application.Commands.ReactivateUser;
using HackatonFiap.Users.Application.Commands.ResetMyPassword;
using HackatonFiap.Users.Application.Errors;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Application.Queries.GetUserById;
using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace HackatonFiap.Users.UnitTests.Commands;

public class UserManagementHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRefreshTokenRepository _refresh = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();

    private static Document Doc() => Document.Create("52998224725", PersonType.Individual);
    private static User Doador() => User.RegisterDonor(PersonType.Individual, Doc(), "D", "d@x.com", new Password("h"));
    private static User Owner() => User.CreateOwner(Doc(), "O", "o@x.com", new Password("h"));
    private static User Gestor() => User.CreateByAdmin(PersonType.Individual, Doc(), "G", "g@x.com", new Password("h"), UserRole.GestorONG);

    // ---- CreateUser ----
    [Fact]
    public async Task CreateUser_CommonGestorCreatingGestor_Fails()
    {
        var h = new CreateUserCommandHandler(_users, _hasher, _audit);
        var result = await h.HandleAsync(new CreateUserCommand(PersonType.Individual, "52998224725", "N", "n@x.com", "Senha@123", UserRole.GestorONG, CallerIsOwner: false, "c"));
        result.Error.Code.Should().Be(UserErrors.CannotManageGestor.Code);
    }

    [Fact]
    public async Task CreateUser_OwnerCreatingGestor_Succeeds()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _users.FindByEmailIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);
        _users.FindByDocumentIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);
        var h = new CreateUserCommandHandler(_users, _hasher, _audit);
        var result = await h.HandleAsync(new CreateUserCommand(PersonType.Individual, "52998224725", "N", "n@x.com", "Senha@123", UserRole.GestorONG, CallerIsOwner: true, "c"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be(UserRole.GestorONG.ToString());
        await _users.Received(1).SaveNewAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task CreateUser_CommonGestorCreatingDoador_Succeeds()
    {
        _hasher.Hash(Arg.Any<string>()).Returns("hashed");
        _users.FindByEmailIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);
        _users.FindByDocumentIncludingInactiveAsync(Arg.Any<string>()).Returns((User?)null);
        var h = new CreateUserCommandHandler(_users, _hasher, _audit);
        var result = await h.HandleAsync(new CreateUserCommand(PersonType.Individual, "52998224725", "N", "n@x.com", "Senha@123", UserRole.Doador, CallerIsOwner: false, "c"));
        result.IsSuccess.Should().BeTrue();
    }

    // ---- Deactivate ----
    [Fact]
    public async Task Deactivate_Owner_FailsImmutable()
    {
        var owner = Owner();
        _users.FindByIdIncludingInactiveAsync(owner.Id).Returns(owner);
        var h = new DeactivateUserCommandHandler(_users, _refresh);
        var result = await h.HandleAsync(new DeactivateUserCommand(owner.Id, CallerIsOwner: true, "c"));
        result.Error.Code.Should().Be(UserErrors.OwnerImmutable.Code);
    }

    [Fact]
    public async Task Deactivate_GestorByCommonGestor_Fails()
    {
        var g = Gestor();
        _users.FindByIdIncludingInactiveAsync(g.Id).Returns(g);
        var h = new DeactivateUserCommandHandler(_users, _refresh);
        var result = await h.HandleAsync(new DeactivateUserCommand(g.Id, CallerIsOwner: false, "c"));
        result.Error.Code.Should().Be(UserErrors.CannotManageGestor.Code);
    }

    [Fact]
    public async Task Deactivate_DoadorByGestor_RevokesTokens()
    {
        var d = Doador();
        _users.FindByIdIncludingInactiveAsync(d.Id).Returns(d);
        var h = new DeactivateUserCommandHandler(_users, _refresh);
        var result = await h.HandleAsync(new DeactivateUserCommand(d.Id, CallerIsOwner: false, "c"));
        result.IsSuccess.Should().BeTrue();
        d.IsActive.Should().BeFalse();
        await _refresh.Received(1).RevokeAllForUserAsync(d.Id);
    }

    [Fact]
    public async Task Deactivate_Unknown_NotFound()
    {
        _users.FindByIdIncludingInactiveAsync(Arg.Any<Guid>()).Returns((User?)null);
        var h = new DeactivateUserCommandHandler(_users, _refresh);
        var result = await h.HandleAsync(new DeactivateUserCommand(Guid.NewGuid(), true, "c"));
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    // ---- Reactivate ----
    [Fact]
    public async Task Reactivate_DoadorByGestor_Succeeds()
    {
        var d = Doador();
        d.Deactivate();
        _users.FindByIdIncludingInactiveAsync(d.Id).Returns(d);
        var h = new ReactivateUserCommandHandler(_users);
        var result = await h.HandleAsync(new ReactivateUserCommand(d.Id, CallerIsOwner: false, "c"));
        result.IsSuccess.Should().BeTrue();
        d.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Reactivate_GestorByCommonGestor_Fails()
    {
        var g = Gestor();
        g.Deactivate();
        _users.FindByIdIncludingInactiveAsync(g.Id).Returns(g);
        var h = new ReactivateUserCommandHandler(_users);
        var result = await h.HandleAsync(new ReactivateUserCommand(g.Id, CallerIsOwner: false, "c"));
        result.Error.Code.Should().Be(UserErrors.CannotManageGestor.Code);
    }

    // ---- ChangeUserRole ----
    [Fact]
    public async Task ChangeRole_NonOwner_Forbidden()
    {
        var h = new ChangeUserRoleCommandHandler(_users, _audit);
        var result = await h.HandleAsync(new ChangeUserRoleCommand(Guid.NewGuid(), UserRole.GestorONG, CallerIsOwner: false, "c"));
        result.Error.Code.Should().Be(UserErrors.Forbidden.Code);
    }

    [Fact]
    public async Task ChangeRole_OwnerPromotesDoador_Succeeds()
    {
        var d = Doador();
        _users.FindByIdIncludingInactiveAsync(d.Id).Returns(d);
        var h = new ChangeUserRoleCommandHandler(_users, _audit);
        var result = await h.HandleAsync(new ChangeUserRoleCommand(d.Id, UserRole.GestorONG, CallerIsOwner: true, "c"));
        result.IsSuccess.Should().BeTrue();
        d.Role.Should().Be(UserRole.GestorONG);
    }

    [Fact]
    public async Task ChangeRole_TargetOwner_Immutable()
    {
        var owner = Owner();
        _users.FindByIdIncludingInactiveAsync(owner.Id).Returns(owner);
        var h = new ChangeUserRoleCommandHandler(_users, _audit);
        var result = await h.HandleAsync(new ChangeUserRoleCommand(owner.Id, UserRole.Doador, CallerIsOwner: true, "c"));
        result.Error.Code.Should().Be(UserErrors.OwnerImmutable.Code);
    }

    // ---- ResetMyPassword ----
    [Fact]
    public async Task ResetPassword_WrongCurrent_Fails()
    {
        var d = Doador();
        _users.FindByIdAsync(d.Id).Returns(d);
        _hasher.Verify("wrong", "h").Returns(false);
        var h = new ResetMyPasswordCommandHandler(_users, _hasher);
        var result = await h.HandleAsync(new ResetMyPasswordCommand(d.Id, "wrong", "Senha@123", "c"));
        result.Error.Code.Should().Be(UserErrors.InvalidCredentials.Code);
    }

    [Fact]
    public async Task ResetPassword_WeakNew_Fails()
    {
        var d = Doador();
        _users.FindByIdAsync(d.Id).Returns(d);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var h = new ResetMyPasswordCommandHandler(_users, _hasher);
        var result = await h.HandleAsync(new ResetMyPasswordCommand(d.Id, "old", "weak", "c"));
        result.Error.Code.Should().Be(UserErrors.InvalidPassword.Code);
    }

    [Fact]
    public async Task ResetPassword_Valid_Succeeds()
    {
        var d = Doador();
        _users.FindByIdAsync(d.Id).Returns(d);
        _hasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _hasher.Hash(Arg.Any<string>()).Returns("newhash");
        var h = new ResetMyPasswordCommandHandler(_users, _hasher);
        var result = await h.HandleAsync(new ResetMyPasswordCommand(d.Id, "old", "Senha@123", "c"));
        result.IsSuccess.Should().BeTrue();
        await _users.Received(1).UpdateAsync(d);
    }

    // ---- GetUserById ----
    [Fact]
    public async Task GetUserById_Unknown_NotFound()
    {
        _users.FindByIdIncludingInactiveAsync(Arg.Any<Guid>()).Returns((User?)null);
        var h = new GetUserByIdQueryHandler(_users);
        var result = await h.HandleAsync(new GetUserByIdQuery(Guid.NewGuid()));
        result.Error.Code.Should().Be(UserErrors.NotFound.Code);
    }

    [Fact]
    public async Task GetUserById_Found_Succeeds()
    {
        var d = Doador();
        _users.FindByIdIncludingInactiveAsync(d.Id).Returns(d);
        var h = new GetUserByIdQueryHandler(_users);
        var result = await h.HandleAsync(new GetUserByIdQuery(d.Id));
        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be("d@x.com");
    }
}
