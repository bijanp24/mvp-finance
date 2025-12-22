using FinanceEngine.Api.Models;

namespace FinanceEngine.Api.Services;

public interface IAccountService
{
    Task<List<AccountDto>> GetAllAccountsAsync();
    Task<AccountDto?> GetAccountByIdAsync(int id);
    Task<AccountDto> CreateAccountAsync(CreateAccountRequest request);
    Task<AccountDto?> UpdateAccountAsync(int id, UpdateAccountRequest request);
    Task<bool> DeleteAccountAsync(int id);
    Task<decimal?> GetAccountBalanceAsync(int id);
}
