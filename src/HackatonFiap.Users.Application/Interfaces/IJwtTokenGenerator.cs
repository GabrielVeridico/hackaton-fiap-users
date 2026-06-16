using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Domain.Entities;

namespace HackatonFiap.Users.Application.Interfaces;

public interface IJwtTokenGenerator
{
    LoginResponse GenerateToken(User user);
}
