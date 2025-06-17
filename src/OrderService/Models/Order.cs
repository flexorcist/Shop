using OrderService.Enums;

namespace OrderService.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.NEW;
}
