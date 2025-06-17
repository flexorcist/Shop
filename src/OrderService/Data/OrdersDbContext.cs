using Microsoft.EntityFrameworkCore;
using OrderService.Models;
using Outbox.Entities;

namespace OrderService.Data;

/// <summary>
/// Контекст БД заказов, использует 'orders'
/// </summary>
public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("orders");

        modelBuilder.Entity<Order>(b =>
        {
            b.ToTable("orders");
            b.HasKey(o => o.Id);
            b.Property(o => o.Amount).HasColumnType("numeric(14,2)");
            b.Property(o => o.Status).HasConversion<string>();
        });
    }
}
