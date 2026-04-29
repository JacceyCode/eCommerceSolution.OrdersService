using Microsoft.Extensions.Hosting;

namespace BusinessLogicLayer.RabbitMQ;

public class RabbitMQProductNameUpdateHostedService : BackgroundService
{
    private readonly IRabbitMQConsumer _consumer;

    public RabbitMQProductNameUpdateHostedService(IRabbitMQConsumer consumer) { 
        _consumer = consumer;
    }

    //public async Task StartAsync(CancellationToken cancellationToken)
    //{
    //    await _consumer.Consume();

    //    // keep this task alive until the application shuts down
    //    await Task.Delay(Timeout.Infinite, cancellationToken);
    //}

    //public async Task StopAsync(CancellationToken cancellationToken)
    //{
    //    await _consumer.DisposeAsync();
    //}

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This runs in the background and does not block the app from startup
        await _consumer.Consume();

        // keep this task alive until the application shuts down
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
