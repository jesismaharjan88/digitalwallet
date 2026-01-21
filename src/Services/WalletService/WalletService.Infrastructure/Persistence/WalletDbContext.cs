using Microsoft.EntityFrameworkCore;
using WalletService.Domain.Entities;

namespace WalletService.Infrastructure.Persistence;

public class WalletDbContext : DbContext
{
    public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
    {
    }

    public DbSet<Wallet> Wallets { get; set; }
    public DbSet<Transaction> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable("wallets");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.Balance).HasColumnName("balance").HasColumnType("decimal(19,4)").HasDefaultValue(0);
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasIndex(e => e.Status);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.ToTable("transactions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.WalletId).HasColumnName("wallet_id").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.Amount).HasColumnName("amount").HasColumnType("decimal(19,4)").IsRequired();
            entity.Property(e => e.BalanceBefore).HasColumnName("balance_before").HasColumnType("decimal(19,4)");
            entity.Property(e => e.BalanceAfter).HasColumnName("balance_after").HasColumnType("decimal(19,4)");
            entity.Property(e => e.Currency).HasColumnName("currency").HasMaxLength(3).HasDefaultValue("USD");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ReferenceId).HasColumnName("reference_id").HasMaxLength(100);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.WalletId);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.CreatedAt);
        });
    }
}