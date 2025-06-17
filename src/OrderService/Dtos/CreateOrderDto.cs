namespace OrderService.Dtos;

public record CreateOrderDto(Guid UserId, decimal Amount, string? Description);
