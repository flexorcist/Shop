using Microsoft.EntityFrameworkCore;
using Outbox.Entities;
using PaymentsService.Models;

namespace PaymentsService.Data;

/// <summary>
/// Контекст платежей, использует схему 'payments'.
/// Имеет входную и выходную Outbox таблицы.
/// </summary>
public class PaymentsDbContext(DbContextOptions<PaymentsDbContext> options) : DbContext(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<PaymentLog> Payments => Set<PaymentLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("payments");

        modelBuilder.Entity<Account>(b =>
        {
            b.ToTable("accounts");
            b.HasKey(a => a.UserId);
            b.Property(a => a.Balance).HasColumnType("numeric(14,2)");
        });

        modelBuilder.Entity<PaymentLog>(b =>
        {
            b.ToTable("payment_logs");
            b.HasKey(p => p.Id);
            b.Property(p => p.Amount).HasColumnType("numeric(14,2)");
            b.Property(p => p.Status).HasConversion<string>();
        });
    }
}
