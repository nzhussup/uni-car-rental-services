using System.Text;
using System.Text.Json;
using CarService.Common;
using CarService.Models.Settings;
using RabbitMQ.Client;

namespace CarService.Services;

public class MessageProducer(RabbitMQSettings rabbitMqSettings) : IMessageProducer
{
    public async Task SendMaintenanceInfoMessageAsync(MaintainanceStartInfo message)
    {
        await SendMessage(message, "car.maintenance");
    }

    public async Task SendCarInfoMessageAsync(CarInfo message)
    {
        await SendMessage(message, "car.info");
    }

    private async Task SendMessage<T>(T message, string routingKey)
    {
        // 1. Create Connection Factory
        var factory = new ConnectionFactory { HostName = rabbitMqSettings.HostName, UserName = rabbitMqSettings.UserName, Password = rabbitMqSettings.Password, Port = rabbitMqSettings.Port };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: rabbitMqSettings.CarExchange, type: ExchangeType.Topic);

        // 3. Serialize and Publish
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var properties = new BasicProperties
        {
            Type = typeof(T).Name,
            Persistent = true // This replaces 'DeliveryMode = 2'
        };

        await channel.BasicPublishAsync(exchange: rabbitMqSettings.CarExchange,
            routingKey: routingKey,
            mandatory: false,
            body: body,
            basicProperties: properties);
    }
}