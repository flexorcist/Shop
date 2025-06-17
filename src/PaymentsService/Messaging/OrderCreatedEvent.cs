using System.Text.Json.Serialization;

namespace PaymentsService.Messaging;

public record OrderCreatedEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    Guid UserId,
    decimal Amount);
