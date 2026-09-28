using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(User user)
    {
        string issuer =
            _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        string audience =
            _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        string secretKey =
            _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "JWT secret key is not configured.");

        int expireMinutes =
            int.Parse(
                _configuration["Jwt:ExpireMinutes"]
                ?? "60");

        List<Claim> claims = new()
        {
            new Claim(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new Claim(
                ClaimTypes.Name,
                user.Username),

            new Claim(
                ClaimTypes.Role,
                user.Role)
        };

        SymmetricSecurityKey key =
            new(
                Encoding.UTF8.GetBytes(secretKey));

        SigningCredentials credentials =
            new(
                key,
                SecurityAlgorithms.HmacSha256);

        JwtSecurityToken token =
            new(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow
                    .AddMinutes(expireMinutes),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}