namespace OrderService.Messaging;

/// <summary>
/// Базовый класс интеграционного события
/// </summary>
public abstract record IntegrationEvent(Guid Id, DateTimeOffset OccurredAt);
