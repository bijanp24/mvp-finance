using FinanceEngine.Data.Entities;

namespace FinanceEngine.Data.Repositories;

public interface IAccountRepository
{
    Task<List<AccountEntity>> GetAllActiveAsync();
    Task<AccountEntity?> GetByIdAsync(int id, bool includeEvents = false, bool includeInactive = false);
    Task<AccountEntity> CreateAsync(AccountEntity account);
    Task<AccountEntity> UpdateAsync(AccountEntity account);
    Task DeleteAsync(AccountEntity account);
}
