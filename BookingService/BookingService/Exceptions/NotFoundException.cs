namespace BookingService.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string entityName, int id)
        : base($"{entityName} with ID {id} not found")
    {
    }
}