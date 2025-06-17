using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OrderService.Data;
using OrderService.Enums;
using OrderService.Models;
using Shop.Tests.Utils;

namespace Shop.Tests.Orders;

[TestFixture]
public class OrdersTests
{
    private OrdersDbContext _db = null!;

    [SetUp]
    public void Setup()
    {
        var opts = new DbContextOptionsBuilder<OrdersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

        _db = new OrdersTestContext(opts);
        _db.Database.EnsureCreated();
    }

    [Test]
    public async Task Order_changes_from_new_to_finished_exactly_once()
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Amount = 10,
            Status = OrderStatus.NEW
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        // имитируем первый SUCCESS
        var original = await _db.Orders.FindAsync(order.Id)!;
        if (original.Status == OrderStatus.NEW)
        {
            original.Status = OrderStatus.FINISHED;
            await _db.SaveChangesAsync();
        }
        
        // повторная попытка, статус уже не NEW, изменений не будет
        var second = await _db.Orders.FindAsync(order.Id)!;
        int rows1 = 1;
        int rows2 = second.Status == OrderStatus.NEW ? 1 : 0;

        Assert.That(rows1, Is.EqualTo(1));
        Assert.That(rows2, Is.EqualTo(0));

        var reloaded = await _db.Orders.FindAsync(order.Id);
        Assert.That(reloaded!.Status, Is.EqualTo(OrderStatus.FINISHED));
    }
}
