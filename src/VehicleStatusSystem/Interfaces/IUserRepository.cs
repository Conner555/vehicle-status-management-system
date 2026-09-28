using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);

    Task<bool> UsernameExistsAsync(string username);

    Task<User> AddAsync(User user);
}