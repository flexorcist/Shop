using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Enums;
using PaymentsService.Messaging;
using PaymentsService.Models;
using Outbox.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PaymentsService.Background;

/// <summary>
/// Слушает события OrderCreated, списывает деньги и отправляет результат
/// </summary>
public class OrderCreatedConsumer(
    IServiceProvider provider,
    ILogger<OrderCreatedConsumer> logger,
    IConnection connection) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = connection.CreateModel();
        channel.ExchangeDeclare("shop.payments", ExchangeType.Fanout, durable: true);
        var queue = channel.QueueDeclare().QueueName;
        channel.QueueBind(queue, "shop.payments", "");

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            try
            {
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();

                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var orderCreated = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

                if (orderCreated == null)
                    return;

                var exists = await db.Payments.AnyAsync(p => p.OrderId == orderCreated.OrderId);
                if (exists)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                await using var tx = await db.Database.BeginTransactionAsync();

                var account = await db.Accounts.FindAsync(orderCreated.UserId);
                if (account is null)
                {
                    db.Payments.Add(new PaymentLog
                    {
                        OrderId = orderCreated.OrderId,
                        UserId = orderCreated.UserId,
                        Amount = orderCreated.Amount,
                        Status = PaymentStatus.FAILED
                    });

                    db.OutboxMessages.Add(new OutboxMessage
                    {
                        Topic = "shop.orders",
                        Type = nameof(PaymentResultEvent),
                        Payload = JsonSerializer.Serialize(new PaymentResultEvent(
                            Guid.NewGuid(), DateTimeOffset.UtcNow, orderCreated.OrderId, "FAILED")),
                        Headers = new(),
                        CreatedAt = DateTimeOffset.UtcNow
                    });

                    await db.SaveChangesAsync();
                    await tx.CommitAsync();
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }

                var rows = await db.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                    UPDATE payments.accounts
                    SET "Balance" = "Balance" - {orderCreated.Amount}
                    WHERE "UserId" = {orderCreated.UserId}
                    AND "Balance" >= {orderCreated.Amount}
                    """
                );

                var success = rows == 1;
                var status = success ? PaymentStatus.SUCCESS : PaymentStatus.FAILED;

                db.Payments.Add(new PaymentLog
                {
                    OrderId = orderCreated.OrderId,
                    UserId = orderCreated.UserId,
                    Amount = orderCreated.Amount,
                    Status = status
                });

                db.OutboxMessages.Add(new OutboxMessage
                {
                    Topic = "shop.orders",
                    Type = nameof(PaymentResultEvent),
                    Payload = JsonSerializer.Serialize(
                        new PaymentResultEvent(Guid.NewGuid(),
                                               DateTimeOffset.UtcNow,
                                               orderCreated.OrderId,
                                               success ? "SUCCESS" : "FAILED")),
                    Headers = new(),
                    CreatedAt = DateTimeOffset.UtcNow
                });

                await db.SaveChangesAsync();
                await tx.CommitAsync();
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка обработки OrderCreated");
            }
        };

        channel.BasicConsume(queue, autoAck: false, consumer);
        return Task.CompletedTask;
    }
}
