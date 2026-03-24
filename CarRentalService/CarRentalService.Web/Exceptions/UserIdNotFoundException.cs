namespace CarRentalService.Exceptions;

public class UserIdNotFoundException : Exception
{
    public UserIdNotFoundException(string message) : base(message)
    {

    }
}