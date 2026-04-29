using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace BusinessLogicLayer.RabbitMQ;

public class RabbitMQConsumer : IAsyncDisposable, IRabbitMQConsumer
{
    private readonly IConfiguration _configuration;
    private readonly ConnectionFactory _connectionFactory;
    private IConnection? _connection;
    private IChannel? _channel;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger<RabbitMQConsumer> _logger;

    public RabbitMQConsumer(IConfiguration configuration, ILogger<RabbitMQConsumer> logger)
    {
        _logger = logger;
        _configuration = configuration;

        string hostname = _configuration["RabbitMQ_HostName"]!;
        string userName = _configuration["RabbitMQ_UserName"]!;
        string password = _configuration["RabbitMQ_Password"]!;
        string port = _configuration["RabbitMQ_Port"]!;

        _connectionFactory = new ConnectionFactory()
        {
            HostName = hostname,
            UserName = userName,
            Password = password,
            Port = int.Parse(port ?? "5672")
        };
    }

    private async Task ConnectAsync()
    {
        if (_connection != null && _connection.IsOpen) return;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection == null || !_connection.IsOpen)
            {
                // Establish a connection to RabbitMQ and create a channel
                _connection = await _connectionFactory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
            }
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task Consume()
    {
        await ConnectAsync();

        string routingKey = "product.update.name";
        string queueName = "orders.product.update.name.queue";
        string exchangeName = _configuration["RabbitMQ_Products_Exchange_Name"]!;

        // Create exchange
        await _channel!.ExchangeDeclareAsync(
            exchange: exchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            arguments: null
            );

        // Create message queue
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
            );

        // Bind the message queue to exchange
        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: exchangeName,
            routingKey: routingKey
            );

        // Create consumer
        AsyncEventingBasicConsumer consumer = new AsyncEventingBasicConsumer(_channel);

        // Process received message
        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                byte[] body = args.Body.ToArray();
                string message = Encoding.UTF8.GetString(body);
                ProductNameUpdateMessage? updateMessage = JsonSerializer.Deserialize<ProductNameUpdateMessage>(message);

                if ( updateMessage != null )
                {
                    _logger.LogInformation($"Product Name Updated: {updateMessage.ProductID}: {updateMessage.ProductName}");
                }

                // Manually Acknowledge (Ack) message after successful processing (standard for microservices)
                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RabbitMQ message: products.name.update");

                // Optional: Nack (Negative Acknowledgement) to requeue the message or send to Dead Letter Exchange
                await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
            }
        };

        // Start consuming
        await _channel.BasicConsumeAsync(
            queue: queueName,
            consumer: consumer,
            autoAck: false
            );
    }


    public async ValueTask DisposeAsync()
    {
        if (_channel != null) await _channel.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
        _connectionLock.Dispose();
    }
}