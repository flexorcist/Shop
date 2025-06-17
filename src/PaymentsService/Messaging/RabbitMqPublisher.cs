using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace PaymentsService.Messaging;

public class RabbitMqPublisher : IDisposable
{
    private readonly IModel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    public RabbitMqPublisher(ILogger<RabbitMqPublisher> logger, IConnection connection)
    {
        _logger = logger;
        _channel = connection.CreateModel();
    }

    public void Publish(string topic, object evt)
    {
        _channel.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        _channel.BasicPublish(exchange: topic, routingKey: "", body: body);
        _logger.LogInformation("=> {EventType} => {Topic}", evt.GetType().Name, topic);
    }

    public void PublishJson(string topic, string json)
    {
        _channel.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);

        var body = Encoding.UTF8.GetBytes(json);
        _channel.BasicPublish(exchange: topic, routingKey: "", body: body);

        _logger.LogInformation("=> raw JSON => {Exchange}", topic);
    }

    public void Dispose() => _channel?.Dispose();
}
