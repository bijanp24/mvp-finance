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
            CalculateBalance(a)
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
            CalculateBalance(account)
        ));
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request, FinanceDbContext db)
    {
        if (!Enum.TryParse<AccountType>(request.Type, true, out var accountType))
            return Results.BadRequest("Invalid account type");

        var account = new AccountEntity
        {
            Name = request.Name,
            Type = accountType,
            InitialBalance = request.InitialBalance,
            AnnualPercentageRate = request.AnnualPercentageRate,
            MinimumPayment = request.MinimumPayment
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
            account.InitialBalance  // New account has no events yet, so current = initial
        ));
    }

    private static async Task<IResult> UpdateAccount(int id, UpdateAccountRequest request, FinanceDbContext db)
    {
        var account = await db.Accounts
            .Include(a => a.Events)
            .FirstOrDefaultAsync(a => a.Id == id && a.IsActive);

        if (account is null)
            return Results.NotFound();

        account.Name = request.Name ?? account.Name;
        account.AnnualPercentageRate = request.AnnualPercentageRate ?? account.AnnualPercentageRate;
        account.MinimumPayment = request.MinimumPayment ?? account.MinimumPayment;

        await db.SaveChangesAsync();

        return Results.Ok(new AccountDto(
            account.Id,
            account.Name,
            account.Type.ToString(),
            account.InitialBalance,
            account.AnnualPercentageRate,
            account.MinimumPayment,
            CalculateBalance(account)
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
    decimal CurrentBalance
);

public record CreateAccountRequest(
    string Name,
    string Type,
    decimal InitialBalance,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null
);

public record UpdateAccountRequest(
    string? Name = null,
    decimal? AnnualPercentageRate = null,
    decimal? MinimumPayment = null
);
