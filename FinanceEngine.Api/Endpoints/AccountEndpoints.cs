using FinanceEngine.Api.Models;
using FinanceEngine.Api.Services;

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

    private static async Task<IResult> GetAllAccounts(IAccountService accountService)
    {
        var accounts = await accountService.GetAllAccountsAsync();
        return Results.Ok(accounts);
    }

    private static async Task<IResult> GetAccountById(int id, IAccountService accountService)
    {
        var account = await accountService.GetAccountByIdAsync(id);
        if (account is null)
            return Results.NotFound();

        return Results.Ok(account);
    }

    private static async Task<IResult> CreateAccount(CreateAccountRequest request, IAccountService accountService)
    {
        try
        {
            var account = await accountService.CreateAccountAsync(request);
            return Results.Created($"/api/accounts/{account.Id}", account);
        }
        catch (ArgumentException ex)
        {
            return ToValidationProblem(ex);
        }
    }

    private static async Task<IResult> UpdateAccount(int id, UpdateAccountRequest request, IAccountService accountService)
    {
        try
        {
            var account = await accountService.UpdateAccountAsync(id, request);
            if (account is null)
                return Results.NotFound();

            return Results.Ok(account);
        }
        catch (ArgumentException ex)
        {
            return ToValidationProblem(ex);
        }
    }

    private static async Task<IResult> DeleteAccount(int id, IAccountService accountService)
    {
        var deleted = await accountService.DeleteAccountAsync(id);
        if (!deleted)
            return Results.NotFound();

        return Results.NoContent();
    }

    private static async Task<IResult> GetAccountBalance(int id, IAccountService accountService)
    {
        var balance = await accountService.GetAccountBalanceAsync(id);
        if (balance is null)
            return Results.NotFound();

        return Results.Ok(new { accountId = id, balance });
    }

    private static IResult ToValidationProblem(ArgumentException ex)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            { "request", new[] { ex.Message } }
        });
    }
}
