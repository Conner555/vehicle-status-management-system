namespace VehicleStatusSystem.Exceptions;

public class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}