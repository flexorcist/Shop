using System.Text.Json.Serialization;

namespace OrderService.Messaging;

public record OrderCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid UserId,
    decimal Amount) : IntegrationEvent(Id, OccurredAt)
{
    [JsonConstructor]
    public OrderCreatedEvent(Guid orderId, Guid userId, decimal amount)
        : this(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, userId, amount) {}
}
