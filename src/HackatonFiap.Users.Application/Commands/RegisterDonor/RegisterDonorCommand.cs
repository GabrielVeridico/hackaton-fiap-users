using HackatonFiap.Users.Domain.Enums;

namespace HackatonFiap.Users.Application.Commands.RegisterDonor;

public record RegisterDonorCommand(
    PersonType PersonType, string Document, string Name, string Email, string Password, string CorrelationId);
