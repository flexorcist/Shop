using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Dtos;
using OrderService.Enums;
using OrderService.Messaging;
using OrderService.Models;
using Outbox.Entities;
using RabbitMQ.Client;

var builder = WebApplication.CreateBuilder(args);

// Конфигурация Pg
var ordersConn = builder.Configuration.GetConnectionString("OrdersDb") ?? "Host=localhost;Username=postgres;Password=postgres;Database=orders";
builder.Services.AddDbContext<OrdersDbContext>(opt => opt.UseNpgsql(ordersConn));

// RabbitMQ
builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory { HostName = builder.Configuration["Rabbit:Host"] ?? "localhost" };
    return factory.CreateConnection();
});

builder.Services.AddHostedService<OrderService.Background.PaymentResultConsumer>();
builder.Services.AddHostedService<OrderService.Background.OutboxDispatcher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSwaggerGen(c => c.EnableAnnotations());

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    db.Database.Migrate();
}

app.UseCors("frontend");
app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/api/orders", async (CreateOrderDto dto, OrdersDbContext db) =>
{
    // Создание заказа + запись Outbox в одной транзакции
    await using var tx = await db.Database.BeginTransactionAsync();
    var order = new Order
    {
        Id = Guid.NewGuid(),
        UserId = dto.UserId,
        Amount = dto.Amount,
        Description = dto.Description
    };
    db.Orders.Add(order);

    var evt = new OrderCreatedEvent(order.Id, order.UserId, order.Amount);

    db.OutboxMessages.Add(new OutboxMessage
    {
        Topic = "shop.payments",
        Key = order.Id.ToString(),
        Type = nameof(OrderCreatedEvent),
        Payload = JsonSerializer.Serialize(evt),
        Headers = new(),
        CreatedAt = DateTimeOffset.UtcNow
    });

    await db.SaveChangesAsync();
    await tx.CommitAsync();
    return Results.Ok(order.Id);
})
.WithName("CreateOrder")
.WithSummary("Создать заказ");

app.MapGet("/api/orders", async (OrdersDbContext db) =>
    await db.Orders.AsNoTracking().ToListAsync());

app.MapGet("/api/orders/{id:guid}", async (Guid id, OrdersDbContext db) =>
{
    var order = await db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();
