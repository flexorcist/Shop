namespace PaymentsService.Models;

/// <summary>
/// Счет пользователя
/// </summary>
public class Account
{
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
}
