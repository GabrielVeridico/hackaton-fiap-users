using HackatonFiap.Users.Domain.Entities;
using HackatonFiap.Users.Domain.Enums;
using HackatonFiap.Users.Domain.ValueObjects;

namespace HackatonFiap.Users.UnitTests.Fixtures;

public static class TestData
{
    public static class Users
    {
        public static User ValidUser => User.RegisterDonor(
            PersonType.Individual,
            Document.Create("52998224725", PersonType.Individual),
            "Test User",
            "test@example.com",
            new Password("$2a$11$hashedpasswordvalue"));

        public static User AnotherUser => User.RegisterDonor(
            PersonType.Individual,
            Document.Create("11144477735", PersonType.Individual),
            "Another User",
            "another@example.com",
            new Password("$2a$11$anotherhashedvalue"));
    }

    public static class Commands
    {
        public static Application.Commands.AuthenticateUser.AuthenticateUserCommand ValidAuthenticate =>
            new("test@example.com", "password123", ValidCorrelationId);
    }

    public static string ValidCorrelationId => Guid.NewGuid().ToString();
}
