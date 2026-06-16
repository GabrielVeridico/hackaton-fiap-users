using FGC.Users.Domain.Entities;
using FGC.Users.Domain.Enums;
using FGC.Users.Domain.ValueObjects;

namespace FGC.Users.UnitTests.Fixtures;

public static class TestData
{
    public static class Users
    {
        public static User ValidUser => User.Create(
            "Test User",
            "test@example.com",
            new Password("$2a$11$hashedpasswordvalue"),
            UserRole.User);

        public static User AnotherUser => User.Create(
            "Another User",
            "another@example.com",
            new Password("$2a$11$anotherhashedvalue"),
            UserRole.User);
    }

    public static class Commands
    {
        public static Application.Commands.CreateUser.CreateUserCommand ValidCreateUser =>
            new("Test User", "newuser@example.com", "ValidPassword123!", ValidCorrelationId);

        public static Application.Commands.AuthenticateUser.AuthenticateUserCommand ValidAuthenticate =>
            new("test@example.com", "password123", ValidCorrelationId);

        public static Application.Commands.UpdateProfile.UpdateProfileCommand ValidUpdateProfile(Guid userId) =>
            new(userId, "Updated Name", null, ValidCorrelationId);
    }

    public static string ValidCorrelationId => Guid.NewGuid().ToString();
}
