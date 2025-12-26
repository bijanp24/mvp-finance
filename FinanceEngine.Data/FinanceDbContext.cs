using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Data;

public class FinanceDbContext : DbContext
{
    public FinanceDbContext(DbContextOptions<FinanceDbContext> options) : base(options)
    {
    }

    public DbSet<AccountEntity> Accounts => Set<AccountEntity>();
    public DbSet<BudgetEntity> Budgets => Set<BudgetEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<FinancialEventEntity> Events => Set<FinancialEventEntity>();
    public DbSet<IncomeScheduleEntity> IncomeSchedules => Set<IncomeScheduleEntity>();
    public DbSet<RecurringContributionEntity> RecurringContributions => Set<RecurringContributionEntity>();
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
            entity.HasIndex(e => e.Status);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Events)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.TargetAccount)
                .WithMany()
                .HasForeignKey(e => e.TargetAccountId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.CategoryId);
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

        // RecurringContribution configuration
        modelBuilder.Entity<RecurringContributionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.SourceAccount)
                .WithMany()
                .HasForeignKey(e => e.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.TargetAccount)
                .WithMany()
                .HasForeignKey(e => e.TargetAccountId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserSettings configuration
        modelBuilder.Entity<UserSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PaycheckAmount).HasPrecision(18, 2);
            entity.Property(e => e.SafetyBuffer).HasPrecision(18, 2);
            entity.HasIndex(e => e.IsActive);
        });

        // Category configuration
        modelBuilder.Entity<CategoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);

            // Seed default categories
            entity.HasData(
                // Recurring expenses
                new CategoryEntity { Id = 1, Name = "Housing", Type = CategoryType.Recurring, Icon = "home", Color = "#5C6BC0", SortOrder = 1, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 2, Name = "Utilities", Type = CategoryType.Recurring, Icon = "bolt", Color = "#FFA726", SortOrder = 2, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 3, Name = "Insurance", Type = CategoryType.Recurring, Icon = "shield", Color = "#26A69A", SortOrder = 3, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 4, Name = "Subscriptions", Type = CategoryType.Recurring, Icon = "subscriptions", Color = "#AB47BC", SortOrder = 4, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 5, Name = "Phone & Internet", Type = CategoryType.Recurring, Icon = "wifi", Color = "#42A5F5", SortOrder = 5, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },

                // One-time/discretionary expenses
                new CategoryEntity { Id = 6, Name = "Groceries", Type = CategoryType.OneTime, Icon = "shopping_cart", Color = "#66BB6A", SortOrder = 10, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 7, Name = "Dining", Type = CategoryType.OneTime, Icon = "restaurant", Color = "#EF5350", SortOrder = 11, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 8, Name = "Transportation", Type = CategoryType.OneTime, Icon = "directions_car", Color = "#78909C", SortOrder = 12, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 9, Name = "Entertainment", Type = CategoryType.OneTime, Icon = "movie", Color = "#EC407A", SortOrder = 13, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 10, Name = "Shopping", Type = CategoryType.OneTime, Icon = "shopping_bag", Color = "#7E57C2", SortOrder = 14, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 11, Name = "Health & Fitness", Type = CategoryType.OneTime, Icon = "fitness_center", Color = "#26C6DA", SortOrder = 15, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 12, Name = "Personal Care", Type = CategoryType.OneTime, Icon = "spa", Color = "#FFCA28", SortOrder = 16, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 13, Name = "Education", Type = CategoryType.OneTime, Icon = "school", Color = "#8D6E63", SortOrder = 17, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 14, Name = "Gifts & Donations", Type = CategoryType.OneTime, Icon = "card_giftcard", Color = "#F48FB1", SortOrder = 18, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true },
                new CategoryEntity { Id = 15, Name = "Other", Type = CategoryType.OneTime, Icon = "more_horiz", Color = "#BDBDBD", SortOrder = 99, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), IsActive = true }
            );
        });

        // Budget configuration
        modelBuilder.Entity<BudgetEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.CategoryId);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.EffectiveDate);

            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.LinkedAccount)
                .WithMany()
                .HasForeignKey(e => e.LinkedAccountId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
