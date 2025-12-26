namespace FinanceEngine.Data.Entities;

public enum CategoryType
{
    Recurring,  // Regular expenses (rent, subscriptions, utilities)
    OneTime     // Discretionary spending (groceries, entertainment)
}

public class CategoryEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string? Icon { get; set; }       // Material icon name (e.g., "shopping_cart")
    public string? Color { get; set; }      // Hex color (e.g., "#4CAF50")
    public int SortOrder { get; set; }      // For UI ordering
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}
