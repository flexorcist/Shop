using System.Text.Json.Serialization;

namespace OrderService.Messaging;

public record PaymentResultEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string Status) : IntegrationEvent(Id, OccurredAt)
{
    [JsonConstructor]
    public PaymentResultEvent(Guid orderId, string status)
        : this(Guid.NewGuid(), DateTimeOffset.UtcNow, orderId, status) {}
}
