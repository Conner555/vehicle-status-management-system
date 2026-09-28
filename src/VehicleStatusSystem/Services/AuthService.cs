using Microsoft.AspNetCore.Identity;
using VehicleStatusSystem.DTOs;
using VehicleStatusSystem.Exceptions;
using VehicleStatusSystem.Interfaces;
using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly ITokenService _tokenService;


    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }


    public async Task<AuthResponseDto> RegisterAsync(
        RegisterDto dto)
    {
        bool exists =
            await _userRepository
                .UsernameExistsAsync(dto.Username);

        if (exists)
        {
            throw new BusinessRuleException(
                $"Username {dto.Username} already exists.");
        }

        User user = new()
        {
            Username = dto.Username,
            Role = "User"
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                dto.Password);

        User created =
            await _userRepository.AddAsync(user);

        string token =
            _tokenService.CreateToken(created);

        return new AuthResponseDto
        {
            UserId = created.Id,
            Username = created.Username,
            Role = created.Role,
            Token = token
        };
    }

    public async Task<AuthResponseDto> LoginAsync(
        LoginDto dto)
    {
        User? user =
            await _userRepository
                .GetByUsernameAsync(dto.Username);

        if (user is null)
        {
            throw new InvalidCredentialsException(
                "Invalid username or password.");
        }

        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                dto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException(
                "Invalid username or password.");
        }

        string token =
            _tokenService.CreateToken(user);

        return new AuthResponseDto
        {
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            Token = token
        };
    }
}