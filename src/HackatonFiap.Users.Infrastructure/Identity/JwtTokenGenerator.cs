using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using HackatonFiap.Users.Application.DTOs;
using HackatonFiap.Users.Application.Interfaces;
using HackatonFiap.Users.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace HackatonFiap.Users.Infrastructure.Identity;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration) => _configuration = configuration;

    public AccessToken GenerateAccessToken(User user)
    {
        var issuer = _configuration.GetValue<string>("Jwt:Issuer") ?? "conexaosolidaria.local";
        var audience = _configuration.GetValue<string>("Jwt:Audience") ?? "conexaosolidaria.clients";
        var secret = _configuration.GetValue<string>("Jwt:Key")
            ?? throw new InvalidOperationException("Jwt:Key must be configured via secret store/Key Vault/env — no insecure fallback.");
        if (Encoding.UTF8.GetByteCount(secret) < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes of high-entropy data.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("isOwner", user.IsOwner ? "true" : "false")
        };

        var expiry = DateTime.UtcNow.AddHours(4);
        var token = new JwtSecurityToken(
            issuer: issuer, audience: audience, claims: claims, expires: expiry,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiry);
    }
}
