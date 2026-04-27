using System.Text;
using System.Text.Json;
using BookingService.Common;
using BookingService.Models.Settings;
using RabbitMQ.Client;

namespace BookingService.Services;

public class MessageProducer(RabbitMQSettings rabbitMqSettings) : IMessageProducer
{
    public async Task SendBookingInfoAsync(BookingInfo bookingInfo)
    {
        await SendMessage(bookingInfo, "booking.info");
    }

    private async Task SendMessage<T>(T message, string routingKey)
    {
        var factory = new ConnectionFactory { HostName = rabbitMqSettings.HostName, UserName = rabbitMqSettings.UserName, Password = rabbitMqSettings.Password, Port = rabbitMqSettings.Port };

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();
        await channel.ExchangeDeclareAsync(exchange: rabbitMqSettings.BookingExchange, type: ExchangeType.Topic);

        // 3. Serialize and Publish
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);
        var properties = new BasicProperties
        {
            Type = typeof(T).Name,
            Persistent = true // This replaces 'DeliveryMode = 2'
        };

        await channel.BasicPublishAsync(exchange: rabbitMqSettings.BookingExchange,
            routingKey: routingKey,
            mandatory: false,
            body: body,
            basicProperties: properties);
    }
}