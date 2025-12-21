using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Data;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<FinancialEventEntity> Events => Set<FinancialEventEntity>();
    public DbSet<IncomeScheduleEntity> IncomeSchedules => Set<IncomeScheduleEntity>();
    public DbSet<UserSettingsEntity> UserSettings => Set<UserSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account configuration
        modelBuilder.Entity<AccountEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.InitialBalance).HasPrecision(18, 2);
            entity.Property(e => e.AnnualPercentageRate).HasPrecision(8, 4);
            entity.Property(e => e.MinimumPayment).HasPrecision(18, 2);

            // Promotional APR and balance transfer fee precision
            entity.Property(e => e.PromotionalAnnualPercentageRate).HasPrecision(8, 4);
            entity.Property(e => e.BalanceTransferFeePercentage).HasPrecision(8, 4);

            entity.HasIndex(e => e.Type);
        });

        // FinancialEvent configuration
        modelBuilder.Entity<FinancialEventEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasIndex(e => e.Date);
            entity.HasIndex(e => e.Type);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Events)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetAccount)
                .WithMany()
                .HasForeignKey(e => e.TargetAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // IncomeSchedule configuration
        modelBuilder.Entity<IncomeScheduleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.TargetAccount)
                .WithMany()
                .HasForeignKey(e => e.TargetAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserSettings configuration
        modelBuilder.Entity<UserSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaycheckAmount).HasPrecision(18, 2);
            entity.Property(e => e.SafetyBuffer).HasPrecision(18, 2);
            entity.HasIndex(e => e.IsActive);
        });
    }
}
