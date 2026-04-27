using System.Text;
using System.Text.Json;
using BookingService.Common;
using BookingService.Models.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingService.Services;

public class CarSubscriber(RabbitMQSettings rabbitMqSettings, IServiceScopeFactory serviceScopeFactory, ILogger<CarSubscriber> logger) : BackgroundService
{
    private IConnection? _connection;
    private IChannel? _channel;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Setup Connection and Channel
        var factory = new ConnectionFactory { HostName = rabbitMqSettings.HostName, UserName = rabbitMqSettings.UserName, Password = rabbitMqSettings.Password, Port = rabbitMqSettings.Port };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        // 2. Declare Exchange (Ensures it exists if consumer starts first)
        string exchangeName = rabbitMqSettings.CarExchange;
        await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, cancellationToken: stoppingToken);

        // 3. Declare the Queue for THIS service
        const string queueName = "car_queue";
        await _channel.QueueDeclareAsync(queue: queueName,
                                        durable: true,
                                        exclusive: false,
                                        autoDelete: false, cancellationToken: stoppingToken);

        // 4. Bind the Queue to the Exchange using the Routing Key
        await _channel.QueueBindAsync(queue: queueName,
                                     exchange: exchangeName,
                                     routingKey: "car.*", cancellationToken: stoppingToken);

        // 5. Setup the Consumer
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);

                // Look at the "Type" header we set in the producer
                var messageType = ea.BasicProperties.Type;

                if (messageType == nameof(CarInfo))
                {
                    var data = JsonSerializer.Deserialize<CarInfo>(messageString);

                    // --- YOUR LOGIC HERE ---
                    using var scope = serviceScopeFactory.CreateScope();
                    var carService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    await carService.HandleCarInfoAsync(data);
                }

                if (messageType == nameof(MaintainanceStartInfo))
                {
                    var data = JsonSerializer.Deserialize<MaintainanceStartInfo>(messageString);

                    // --- YOUR LOGIC HERE ---
                    using var scope = serviceScopeFactory.CreateScope();
                    var carService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                    await carService.HandleMaintainanceInfoAsync(data);
                }

                // 6. Manually Acknowledge the message (Safe approach)
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
            }

        };

        // 7. Start Consuming
        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

        // Keep the service alive until the app shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Cleanup resources on shutdown
        if (_channel != null) await _channel.CloseAsync(cancellationToken: cancellationToken);
        if (_connection != null) await _connection.CloseAsync(cancellationToken: cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}