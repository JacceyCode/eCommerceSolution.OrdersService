namespace BusinessLogicLayer.RabbitMQ;

public interface IRabbitMQConsumer
{
    Task Consume();
    //ValueTask DisposeAsync();
}