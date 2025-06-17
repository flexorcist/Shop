using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Enums;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderService.Background;

public class PaymentResultConsumer : BackgroundService
{
    private readonly IServiceProvider _provider;
    private readonly ILogger<PaymentResultConsumer> _log;
    private readonly IConnection _conn;

    public PaymentResultConsumer(IServiceProvider provider,
                                 ILogger<PaymentResultConsumer> log,
                                 IConnection conn)
    {
        _provider = provider;
        _log = log;
        _conn = conn;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var ch = _conn.CreateModel();
        ch.ExchangeDeclare("shop.orders", ExchangeType.Fanout, durable: true);
        var queue = ch.QueueDeclare().QueueName;
        ch.QueueBind(queue, "shop.orders", "");

        var consumer = new EventingBasicConsumer(ch);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                using var doc = JsonDocument.Parse(json);

                var orderId = doc.RootElement.GetProperty("OrderId").GetGuid();
                var status = doc.RootElement.GetProperty("Status").GetString();

                var newStatus = status == "SUCCESS" ? OrderStatus.FINISHED : OrderStatus.CANCELLED;

                using var scope = _provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();

                var rows = await db.Database.ExecuteSqlInterpolatedAsync
                ($"""
                    UPDATE orders.orders
                    SET "Status" = {newStatus}
                    WHERE "Id" = {orderId}
                      AND "Status" = 'NEW'
                 """);

                if (rows == 1)
                    _log.LogInformation("Заказ {OrderId} => {Status}", orderId, newStatus);

                ch.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Ошибка обработки PaymentResult");
            }
        };

        ch.BasicConsume(queue, autoAck: false, consumer);
        return Task.CompletedTask;
    }
}
