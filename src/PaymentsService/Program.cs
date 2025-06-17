using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using RabbitMQ.Client;
using PaymentsService.Messaging;

var builder = WebApplication.CreateBuilder(args);

var paymentsConn = builder.Configuration.GetConnectionString("PaymentsDb") ?? "Host=localhost;Username=postgres;Password=postgres;Database=payments";
builder.Services.AddDbContext<PaymentsDbContext>(opt => opt.UseNpgsql(paymentsConn));

builder.Services.AddSingleton<IConnection>(_ =>
{
    var factory = new ConnectionFactory { HostName = builder.Configuration["Rabbit:Host"] ?? "localhost" };
    return factory.CreateConnection();
});

builder.Services.AddHostedService<PaymentsService.Background.OrderCreatedConsumer>();
builder.Services.AddHostedService<PaymentsService.Background.OutboxDispatcher>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCors(opt =>
{
    opt.AddPolicy("frontend", p =>
        p.AllowAnyOrigin()
         .AllowAnyHeader()
         .AllowAnyMethod());
});
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
    db.Database.Migrate();
}

app.UseCors("frontend");
app.UseSwagger();
app.UseSwaggerUI();

// API
app.MapPost("/api/accounts", async (Guid userId, PaymentsDbContext db) =>
{
    if (await db.Accounts.FindAsync(userId) is not null)
        return Results.Conflict("Account already exists");

    db.Accounts.Add(new PaymentsService.Models.Account { UserId = userId, Balance = 0 });
    await db.SaveChangesAsync();
    return Results.Created($"/api/accounts/{userId}", null);
});

app.MapPost("/api/accounts/{userId:guid}/top-up", async (Guid userId, decimal amount, PaymentsDbContext db) =>
{
    var account = await db.Accounts.FindAsync(userId);
    if (account is null) return Results.NotFound();

    account.Balance += amount;
    await db.SaveChangesAsync();
    return Results.Ok(account.Balance);
});

app.MapGet("/api/accounts/{userId:guid}", async (Guid userId, PaymentsDbContext db) =>
{
    var account = await db.Accounts.FindAsync(userId);
    return account is null ? Results.NotFound() : Results.Ok(account);
});

app.Run();
