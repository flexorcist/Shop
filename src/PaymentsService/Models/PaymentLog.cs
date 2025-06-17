using PaymentsService.Enums;

namespace PaymentsService.Models;

public class PaymentLog
{
    public long Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
