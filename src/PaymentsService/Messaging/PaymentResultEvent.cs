using System.Text.Json.Serialization;

namespace PaymentsService.Messaging;

public record PaymentResultEvent(
    Guid Id,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string Status);
