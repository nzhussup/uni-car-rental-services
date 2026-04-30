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
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = rabbitMqSettings.HostName,
            UserName = rabbitMqSettings.UserName,
            Password = rabbitMqSettings.Password,
            Port = rabbitMqSettings.Port
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                string exchangeName = rabbitMqSettings.CarExchange;
                await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: ExchangeType.Topic, cancellationToken: stoppingToken);

                const string queueName = "car_queue";
                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                await _channel.QueueBindAsync(
                    queue: queueName,
                    exchange: exchangeName,
                    routingKey: "car.*",
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var messageString = Encoding.UTF8.GetString(body);
                        var messageType = ea.BasicProperties.Type;

                        if (messageType == nameof(CarInfo))
                        {
                            var data = JsonSerializer.Deserialize<CarInfo>(messageString);
                            using var scope = serviceScopeFactory.CreateScope();
                            var carService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                            await carService.HandleCarInfoAsync(data);
                        }

                        if (messageType == nameof(MaintainanceStartInfo))
                        {
                            var data = JsonSerializer.Deserialize<MaintainanceStartInfo>(messageString);
                            using var scope = serviceScopeFactory.CreateScope();
                            var carService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                            await carService.HandleMaintainanceInfoAsync(data);
                        }

                        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, e.Message);
                    }
                };

                await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
                logger.LogInformation("Connected to RabbitMQ at {Host}:{Port} and listening on queue {QueueName}", rabbitMqSettings.HostName, rabbitMqSettings.Port, queueName);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "RabbitMQ subscriber connection failed. Retrying in {DelaySeconds} seconds.", RetryDelay.TotalSeconds);
                await CleanupAsync(stoppingToken);
                await Task.Delay(RetryDelay, stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await CleanupAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }

    private async Task CleanupAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken: cancellationToken);
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.CloseAsync(cancellationToken: cancellationToken);
            _connection = null;
        }
    }
}
