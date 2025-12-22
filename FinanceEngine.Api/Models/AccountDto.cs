namespace FinanceEngine.Api.Models;

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
