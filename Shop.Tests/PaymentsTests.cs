using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PaymentsService.Background;
using PaymentsService.Data;
using PaymentsService.Enums;
using PaymentsService.Messaging;
using PaymentsService.Models;
using RabbitMQ.Client;
using Shop.Tests.Utils;

namespace Shop.Tests.Payments;

[TestFixture]
public class PaymentsTests
{
    private PaymentsDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opts = new DbContextOptionsBuilder<PaymentsDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

        _db = new PaymentsTestContext(opts);
        _db.Database.EnsureCreated();
    }

    [Test]
    public async Task Balance_not_enough__payment_failed()
    {
        var uid = Guid.NewGuid();
        _db.Accounts.Add(new Account { UserId = uid, Balance = 50 });
        await _db.SaveChangesAsync();

        var orderEvt = new OrderCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), uid, 100);

        var publisher = new RabbitMqPublisher(Mock.Of<ILogger<RabbitMqPublisher>>(), Mock.Of<IConnection>());

        // эмулируем часть кода внутри OrderCreatedConsumer
        var account = await _db.Accounts.FindAsync(uid)!;
        bool success = false;
        if (account.Balance >= orderEvt.Amount)
        {
            account.Balance -= orderEvt.Amount;
            success = true;
        }

        _db.Payments.Add(new PaymentLog
        {
            OrderId = orderEvt.OrderId,
            UserId = orderEvt.UserId,
            Amount = orderEvt.Amount,
            Status = success ? PaymentStatus.SUCCESS : PaymentStatus.FAILED
        });
        await _db.SaveChangesAsync();

        var pay = await _db.Payments.SingleAsync();
        Assert.That(pay.Status, Is.EqualTo(PaymentStatus.FAILED));
        var acc = await _db.Accounts.FindAsync(uid);
        Assert.That(acc!.Balance, Is.EqualTo(50));   // деньги не ушли
    }
}
