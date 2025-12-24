using FinanceEngine.Api.Models;
using FinanceEngine.Data.Entities;
using FinanceEngine.Data.Repositories;
using FinanceEngine.Services;

namespace FinanceEngine.Api.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accountRepository;

    public AccountService(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<List<AccountDto>> GetAllAccountsAsync()
    {
        var accounts = await _accountRepository.GetAllActiveAsync();
        return accounts.Select(MapToDto).ToList();
    }

    public async Task<AccountDto?> GetAccountByIdAsync(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id, includeEvents: true);
        return account == null ? null : MapToDto(account);
    }

    public async Task<AccountDto> CreateAccountAsync(CreateAccountRequest request)
    {
        if (!Enum.TryParse<Data.Entities.AccountType>(request.Type, true, out var accountType))
            throw new ArgumentException("Invalid account type");

        ValidateAccountRequest(request);

        // Auto-calculate minimum payment for debt accounts if not provided
        var minimumPayment = request.MinimumPayment;
        if (accountType == Data.Entities.AccountType.Debt && !minimumPayment.HasValue && request.InitialBalance > 0)
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

        var created = await _accountRepository.CreateAsync(account);
        return MapToDto(created);
    }

    public async Task<AccountDto?> UpdateAccountAsync(int id, UpdateAccountRequest request)
    {
        var account = await _accountRepository.GetByIdAsync(id, includeEvents: true);
        if (account == null)
            return null;

        ValidateAccountUpdateRequest(request);

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

        var updated = await _accountRepository.UpdateAsync(account);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id, includeInactive: true);
        if (account == null)
            return false;

        if (!account.IsActive)
            return true;

        await _accountRepository.DeleteAsync(account);
        return true;
    }

    public async Task<decimal?> GetAccountBalanceAsync(int id)
    {
        var account = await _accountRepository.GetByIdAsync(id, includeEvents: true);
        if (account == null)
            return null;

        return CalculateBalance(account);
    }

    private AccountDto MapToDto(AccountEntity account)
    {
        return new AccountDto(
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
        );
    }

    private decimal CalculateBalance(AccountEntity account)
    {
        var accountType = MapAccountType(account.Type);
        var events = account.Events
            .Select(e => new { Type = MapEventType(e.Type), e.Amount })
            .Where(e => e.Type.HasValue)
            .Select(e => new FinancialEvent(e.Type!.Value, e.Amount));
        return BalanceCalculator.Calculate(accountType, account.InitialBalance, events);
    }

    private static FinanceEngine.Services.AccountType MapAccountType(Data.Entities.AccountType entityType)
    {
        return Enum.TryParse<FinanceEngine.Services.AccountType>(entityType.ToString(), out var mapped)
            ? mapped
            : FinanceEngine.Services.AccountType.Cash;
    }

    private static FinanceEngine.Services.EventType? MapEventType(Data.Entities.EventType entityType)
    {
        return Enum.TryParse<FinanceEngine.Services.EventType>(entityType.ToString(), out var mapped)
            ? mapped
            : null;
    }

    private decimal? CalculateEffectiveAPR(AccountEntity account)
    {
        if (account.PromotionalPeriodEndDate.HasValue &&
            account.PromotionalPeriodEndDate.Value > DateTime.UtcNow &&
            account.PromotionalAnnualPercentageRate.HasValue)
        {
            return account.PromotionalAnnualPercentageRate.Value;
        }
        return account.AnnualPercentageRate;
    }

    private void ValidateAccountRequest(CreateAccountRequest request)
    {
        if (request.StatementDayOfMonth is < 1 or > 31)
            throw new ArgumentException("Statement day must be 1-31");

        if (request.PaymentDueDayOfMonth is < 1 or > 31)
            throw new ArgumentException("Payment due day must be 1-31");

        if (request.AnnualPercentageRate is < 0 or > 1)
            throw new ArgumentException("APR must be between 0 and 1");

        if (request.PromotionalAnnualPercentageRate is < 0 or > 1)
            throw new ArgumentException("Promotional APR must be between 0 and 1");

        if (request.PromotionalAnnualPercentageRate.HasValue != request.PromotionalPeriodEndDate.HasValue)
            throw new ArgumentException("Both promotional APR and end date required, or neither");

        if (request.PromotionalPeriodEndDate.HasValue &&
            request.PromotionalPeriodEndDate.Value <= DateTime.UtcNow)
            throw new ArgumentException("Promotional end date must be in the future");

        if (request.BalanceTransferFeePercentage is < 0 or > 1)
            throw new ArgumentException("Balance transfer fee must be between 0 and 1");
    }

    private void ValidateAccountUpdateRequest(UpdateAccountRequest request)
    {
        if (request.StatementDayOfMonth is < 1 or > 31)
            throw new ArgumentException("Statement day must be 1-31");

        if (request.PaymentDueDayOfMonth is < 1 or > 31)
            throw new ArgumentException("Payment due day must be 1-31");

        if (request.AnnualPercentageRate is < 0 or > 1)
            throw new ArgumentException("APR must be between 0 and 1");

        if (request.PromotionalAnnualPercentageRate is < 0 or > 1)
            throw new ArgumentException("Promotional APR must be between 0 and 1");

        if (request.PromotionalAnnualPercentageRate.HasValue != request.PromotionalPeriodEndDate.HasValue)
            throw new ArgumentException("Both promotional APR and end date required, or neither");

        if (request.PromotionalPeriodEndDate.HasValue &&
            request.PromotionalPeriodEndDate.Value <= DateTime.UtcNow)
            throw new ArgumentException("Promotional end date must be in the future");

        if (request.BalanceTransferFeePercentage is < 0 or > 1)
            throw new ArgumentException("Balance transfer fee must be between 0 and 1");
    }
}
