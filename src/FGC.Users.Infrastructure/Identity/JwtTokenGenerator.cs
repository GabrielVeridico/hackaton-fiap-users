using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FGC.Users.Application.DTOs;
using FGC.Users.Application.Interfaces;
using FGC.Users.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FGC.Users.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse GenerateToken(User user)
    {
        var issuer = _configuration.GetValue<string>("Jwt:Issuer") ?? "fgc.local";
        var audience = _configuration.GetValue<string>("Jwt:Audience") ?? "fgc.clients";
        var secret = _configuration.GetValue<string>("Jwt:Key") ?? "super-secret-key-for-dev-environment-only";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("role", user.Role.ToString())
        };

        var expiry = DateTime.UtcNow.AddHours(1);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
