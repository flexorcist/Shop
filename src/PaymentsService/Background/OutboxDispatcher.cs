using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Outbox.Entities;
using PaymentsService.Data;
using PaymentsService.Messaging;
using RabbitMQ.Client;

namespace PaymentsService.Background;

/// <summary>
/// Публикация сообщений Outbox
/// </summary>
public class OutboxDispatcher(
    IServiceProvider provider,
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
                using var scope = provider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
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
                    var doc = JsonSerializer.Deserialize<JsonElement>(msg.Payload);
                    object evt = doc;
                    publisher.Publish(msg.Topic, evt);
                    db.OutboxMessages.Remove(msg);
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка публикации Outbox");
            }
        }
    }
}
