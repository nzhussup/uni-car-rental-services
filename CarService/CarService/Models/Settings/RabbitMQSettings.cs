namespace CarService.Models.Settings;

public class RabbitMQSettings
{
    public required string HostName { get; set; }
    public required int Port { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string CarExchange { get; set; }
    public required string BookingExchange { get; set; }
}