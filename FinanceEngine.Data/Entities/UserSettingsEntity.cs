namespace FinanceEngine.Data.Entities;

public enum PayFrequency
{
    Weekly = 7,
    BiWeekly = 14,
    SemiMonthly = 15,
    Monthly = 30
}

public class UserSettingsEntity
{
    public int Id { get; set; }
    public PayFrequency PayFrequency { get; set; } = PayFrequency.BiWeekly;
    public decimal PaycheckAmount { get; set; } = 2500m;
    public decimal SafetyBuffer { get; set; } = 100m;
    public DateTime? NextPaycheckDate { get; set; }  // Anchor for calculating future paychecks
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
