using Microsoft.EntityFrameworkCore;
using Outbox.Entities;
using OrderService.Data;
using PaymentsService.Data;

namespace Shop.Tests.Utils;

internal class OrdersTestContext : OrdersDbContext
{
    public OrdersTestContext(DbContextOptions<OrdersDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.Ignore<OutboxMessage>();
    }
}

internal class PaymentsTestContext : PaymentsDbContext
{
    public PaymentsTestContext(DbContextOptions<PaymentsDbContext> opts) : base(opts) { }

    protected override void OnModelCreating(ModelBuilder mb)
    {
        base.OnModelCreating(mb);
        mb.Ignore<OutboxMessage>();
    }
}
