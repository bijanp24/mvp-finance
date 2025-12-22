using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Data.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly FinanceDbContext _context;

    public AccountRepository(FinanceDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountEntity>> GetAllActiveAsync()
    {
        return await _context.Accounts
            .Where(a => a.IsActive)
            .Include(a => a.Events)
            .ToListAsync();
    }

    public async Task<AccountEntity?> GetByIdAsync(int id, bool includeEvents = false, bool includeInactive = false)
    {
        var query = _context.Accounts
            .Where(a => a.Id == id);

        if (!includeInactive)
        {
            query = query.Where(a => a.IsActive);
        }

        if (includeEvents)
        {
            query = query.Include(a => a.Events);
        }

        return await query.FirstOrDefaultAsync();
    }

    public async Task<AccountEntity> CreateAsync(AccountEntity account)
    {
        _context.Accounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task<AccountEntity> UpdateAsync(AccountEntity account)
    {
        _context.Accounts.Update(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task DeleteAsync(AccountEntity account)
    {
        account.IsActive = false;
        await _context.SaveChangesAsync();
    }
}
