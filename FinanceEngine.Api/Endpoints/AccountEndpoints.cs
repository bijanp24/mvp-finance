using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetAllAccounts);
        group.MapGet("/{id}", GetAccountById);
        group.MapPost("/", CreateAccount);
        group.MapPut("/{id}", UpdateAccount);
        group.MapDelete("/{id}", DeleteAccount);
        group.MapGet("/{id}/balance", GetAccountBalance);

        return group;
    }

    private static async Task<IResult> GetAllAccounts(FinanceDbContext db)
    {
        var accounts = await db.Accounts
            .Where(a => a.IsActive)
            .Include(a => a.Events)
            .ToListAsync();

        var accountDtos = accounts.Select(a => new AccountDto(
            a.Id,
            a.Name,
            a.Type.ToString(),
            a.InitialBalance,
            a.AnnualPercentageRate,
            a.MinimumPayment,
            CalculateBalance(a),
            a.PromotionalAnnualPercentageRate,
            a.PromotionalPeriodEndDate,
            a.BalanceTransferFeePercentage,
            a.StatementDayOfMonth,
            a.StatementDateOverride,
            a.PaymentDueDayOfMonth,
            a.PaymentDueDateOverride,
            CalculateEffectiveAPR(a)
        )).ToList();

        return Results.Ok(accountDtos);
    }

    private static decimal CalculateBalance(AccountEntity account)
    {
        var balance = account.InitialBalance;
        foreach (var evt in account.Events)
        {
            balance += evt.Type switch
            {
                EventType.Income => evt.Amount,
                EventType.Expense => -evt.Amount,
                EventType.DebtCharge => evt.Amount,
                EventType.DebtPayment => -evt.Amount,
                EventType.InterestFee => evt.Amount,
                EventType.SavingsContribution => evt.Amount,
                EventType.InvestmentContribution => evt.Amount,
                _ => 0
            };
        }
        return balance;
    }

    private static decimal? CalculateEffectiveAPR(AccountEntity account)
    {
        if (account.PromotionalPeriodEndDate.HasValue &&
            account.PromotionalPeriodEndDate.Value > DateTime.UtcNow &&
            account.PromotionalAnnualPercentageRate.HasValue)
        {
            return account.PromotionalAnnualPercentageRate.Value;
        }
        return account.AnnualPercentageRate;
    }

    private static async Task<IResult> GetAccountById(int id, FinanceDbContext db)
    {
        var account = await db.Accounts
            .Include(a => a.Events)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

        if (account is null)
            return Results.NotFound();

        return Results.Ok(new AccountDto(
            account.Id,
            account.Name,
            account.Type.ToString(),
            account.InitialBalance,
            account.AnnualPercentageRate,
            account.MinimumPayment,
            CalculateBalance(account),
            account.PromotionalAnnualPercentageRate,
            account.PromotionalPeriodEndDate,
            account.BalanceTransferFeePercentage,
            account.StatementDayOfMonth,
            account.StatementDateOverride,
            account.PaymentDueDayOfMonth,
            account.PaymentDueDateOverride,
            CalculateEffectiveAPR(account)
        ));
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request, FinanceDbContext db)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
            return Results.BadRequest("Invalid account type");

        // Validation for new debt fields
        if (request.StatementDayOfMonth is < 1 or > 31)
            return Results.BadRequest("Statement day must be 1-31");

        if (request.PaymentDueDayOfMonth is < 1 or > 31)
            return Results.BadRequest("Payment due day must be 1-31");

        if (request.PromotionalAnnualPercentageRate.HasValue != request.PromotionalPeriodEndDate.HasValue)
            return Results.BadRequest("Both promotional APR and end date required, or neither");

        if (request.PromotionalPeriodEndDate.HasValue &&
            request.PromotionalPeriodEndDate.Value <= DateTime.UtcNow)
            return Results.BadRequest("Promotional end date must be in the future");

        if (request.BalanceTransferFeePercentage is < 0 or > 100)
            return Results.BadRequest("Balance transfer fee must be 0-100%");

        // Auto-calculate minimum payment for debt accounts if not provided
        var minimumPayment = request.MinimumPayment;
        if (accountType == AccountType.Debt && !minimumPayment.HasValue && request.InitialBalance > 0)
        {
            // Check if 0% promo is active
            var hasActivePromo = request.PromotionalAnnualPercentageRate == 0 &&
                                request.PromotionalPeriodEndDate.HasValue &&
                                request.PromotionalPeriodEndDate.Value > DateTime.UtcNow;

            // Use 2% for 0% promo, 4% otherwise
            var percentage = hasActivePromo ? 0.02m : 0.04m;
            minimumPayment = Math.Round(request.InitialBalance * percentage, 2);
        }

        var account = new AccountEntity
        {
            Name = request.Name,
            Type = accountType,
            InitialBalance = request.InitialBalance,
            AnnualPercentageRate = request.AnnualPercentageRate,
            MinimumPayment = minimumPayment,
            PromotionalAnnualPercentageRate = request.PromotionalAnnualPercentageRate,
            PromotionalPeriodEndDate = request.PromotionalPeriodEndDate,
            BalanceTransferFeePercentage = request.BalanceTransferFeePercentage,
            StatementDayOfMonth = request.StatementDayOfMonth,
            StatementDateOverride = request.StatementDateOverride,
            PaymentDueDayOfMonth = request.PaymentDueDayOfMonth,
            PaymentDueDateOverride = request.PaymentDueDateOverride
        };

        db.Accounts.Add(account);
        await db.SaveChangesAsync();

        return Results.Created($"/api/accounts/{account.Id}", new AccountDto(
            account.Id,
            account.Name,
            account.Type.ToString(),
            account.InitialBalance,
            account.AnnualPercentageRate,
            account.MinimumPayment,
            account.InitialBalance,  // New account has no events yet, so current = initial
            account.PromotionalAnnualPercentageRate,
            account.PromotionalPeriodEndDate,
            account.BalanceTransferFeePercentage,
            account.StatementDayOfMonth,
            account.StatementDateOverride,
            account.PaymentDueDayOfMonth,
            account.PaymentDueDateOverride,
            CalculateEffectiveAPR(account)
        ));
    }

    private static async Task<IResult> UpdateAccount(int id, UpdateAccountRequest request, FinanceDbContext db)
    {
        var account = await db.Accounts
            .Include(a => a.Events)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

        if (account is null)
            return Results.NotFound();

        // Validation for new debt fields
        if (request.StatementDayOfMonth is < 1 or > 31)
            return Results.BadRequest("Statement day must be 1-31");

        if (request.PaymentDueDayOfMonth is < 1 or > 31)
            return Results.BadRequest("Payment due day must be 1-31");

        if (request.PromotionalAnnualPercentageRate.HasValue != request.PromotionalPeriodEndDate.HasValue)
            return Results.BadRequest("Both promotional APR and end date required, or neither");

        if (request.PromotionalPeriodEndDate.HasValue &&
            request.PromotionalPeriodEndDate.Value <= DateTime.UtcNow)
            return Results.BadRequest("Promotional end date must be in the future");

        if (request.BalanceTransferFeePercentage is < 0 or > 100)
            return Results.BadRequest("Balance transfer fee must be 0-100%");

        account.Name = request.Name ?? account.Name;
        account.AnnualPercentageRate = request.AnnualPercentageRate ?? account.AnnualPercentageRate;
        account.MinimumPayment = request.MinimumPayment ?? account.MinimumPayment;
        account.PromotionalAnnualPercentageRate = request.PromotionalAnnualPercentageRate ?? account.PromotionalAnnualPercentageRate;
        account.PromotionalPeriodEndDate = request.PromotionalPeriodEndDate ?? account.PromotionalPeriodEndDate;
        account.BalanceTransferFeePercentage = request.BalanceTransferFeePercentage ?? account.BalanceTransferFeePercentage;
        account.StatementDayOfMonth = request.StatementDayOfMonth ?? account.StatementDayOfMonth;
        account.StatementDateOverride = request.StatementDateOverride ?? account.StatementDateOverride;
        account.PaymentDueDayOfMonth = request.PaymentDueDayOfMonth ?? account.PaymentDueDayOfMonth;
        account.PaymentDueDateOverride = request.PaymentDueDateOverride ?? account.PaymentDueDateOverride;

        await db.SaveChangesAsync();

        return Results.Ok(new AccountDto(
            account.Id,
            account.Name,
            account.Type.ToString(),
            account.InitialBalance,
            account.AnnualPercentageRate,
            account.MinimumPayment,
            CalculateBalance(account),
            account.PromotionalAnnualPercentageRate,
            account.PromotionalPeriodEndDate,
            account.BalanceTransferFeePercentage,
            account.StatementDayOfMonth,
            account.StatementDateOverride,
            account.PaymentDueDayOfMonth,
            account.PaymentDueDateOverride,
            CalculateEffectiveAPR(account)
        ));
    }

    private static async Task<IResult> DeleteAccount(int id, FinanceDbContext db)
    {
        var account = await db.Accounts.FindAsync(id);
        if (account is null)
            return Results.NotFound();

        account.IsActive = false;
        await db.SaveChangesAsync();

        return Results.NoContent();
    }

    private static async Task<IResult> GetAccountBalance(int id, FinanceDbContext db)
    {
        var account = await db.Accounts
            .Include(a => a.Events)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

        if (account is null)
            return Results.NotFound();

        // Calculate balance from initial balance + events (event sourcing)
        var balance = account.InitialBalance;
        foreach (var evt in account.Events)
        {
            balance += evt.Type switch
            {
                EventType.Income => evt.Amount,
                EventType.Expense => -evt.Amount,
                EventType.DebtCharge => evt.Amount,
                EventType.DebtPayment => -evt.Amount,
                EventType.InterestFee => evt.Amount,
                EventType.SavingsContribution => evt.Amount,
                EventType.InvestmentContribution => evt.Amount,
                _ => 0
            };
        }

        return Results.Ok(new { accountId = id, balance });
    }
}

// DTOs
public record AccountDto(
    int Id,
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? AnnualPercentageRate,
    decimal? MinimumPayment,
    decimal CurrentBalance,
    decimal? PromotionalAnnualPercentageRate,
    DateTime? PromotionalPeriodEndDate,
    decimal? BalanceTransferFeePercentage,
    int? StatementDayOfMonth,
    DateTime? StatementDateOverride,
    int? PaymentDueDayOfMonth,
    DateTime? PaymentDueDateOverride,
    decimal? EffectiveAnnualPercentageRate
);

public record CreateAccountRequest(
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null,
    decimal? PromotionalAnnualPercentageRate = null,
    DateTime? PromotionalPeriodEndDate = null,
    decimal? BalanceTransferFeePercentage = null,
    int? StatementDayOfMonth = null,
    DateTime? StatementDateOverride = null,
    int? PaymentDueDayOfMonth = null,
    DateTime? PaymentDueDateOverride = null
);

public record UpdateAccountRequest(
    string? Name = null,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null,
    decimal? PromotionalAnnualPercentageRate = null,
    DateTime? PromotionalPeriodEndDate = null,
    decimal? BalanceTransferFeePercentage = null,
    int? StatementDayOfMonth = null,
    DateTime? StatementDateOverride = null,
    int? PaymentDueDayOfMonth = null,
    DateTime? PaymentDueDateOverride = null
);
