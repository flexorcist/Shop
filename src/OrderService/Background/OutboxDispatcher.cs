using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Messaging;
using Outbox.Entities;
using Outbox.Extensions;
using RabbitMQ.Client;

namespace OrderService.Background;

/// <summary>
/// Периодически вычитывает Outbox и публикует сообщения в RabbitMQ.
/// </summary>
public class OutboxDispatcher(
    IServiceProvider serviceProvider,
    ILogger<OutboxDispatcher> logger,
    IConnection connection) : BackgroundService
{
    private readonly TimeSpan _delay = TimeSpan.FromSeconds(5);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<RabbitMqPublisher>();

                var messages = await db.OutboxMessages
                    .OrderBy(m => m.Id)
                    .Take(50)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(_delay, stoppingToken);
                    continue;
                }

                foreach (var msg in messages)
                {
                    publisher.PublishJson(msg.Topic, msg.Payload);
                    db.OutboxMessages.Remove(msg);
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при отправке сообщений Outbox");
            }
        }
    }
}
