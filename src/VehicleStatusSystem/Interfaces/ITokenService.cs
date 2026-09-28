using VehicleStatusSystem.Models;

namespace VehicleStatusSystem.Interfaces;

public interface ITokenService
{
    string CreateToken(User user);
}