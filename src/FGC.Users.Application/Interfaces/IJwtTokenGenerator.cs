using FGC.Users.Application.DTOs;
using FGC.Users.Domain.Entities;

namespace FGC.Users.Application.Interfaces;

public interface IJwtTokenGenerator
{
    LoginResponse GenerateToken(User user);
}
