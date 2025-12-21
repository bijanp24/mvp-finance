using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class SettingsEndpoints
{
    public static RouteGroupBuilder MapSettingsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetSettings);
        group.MapPut("/", UpdateSettings);
        return group;
    }

    private static async Task<IResult> GetSettings(FinanceDbContext db)
    {
        // Get the most recent active settings, or return defaults
        var settings = await db.UserSettings
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            // Return default settings
            return Results.Ok(new SettingsDto(
                PayFrequency: "BiWeekly",
                PaycheckAmount: 2500m,
                SafetyBuffer: 100m,
                NextPaycheckDate: null
            ));
        }

        return Results.Ok(new SettingsDto(
            PayFrequency: settings.PayFrequency.ToString(),
            PaycheckAmount: settings.PaycheckAmount,
            SafetyBuffer: settings.SafetyBuffer,
            NextPaycheckDate: settings.NextPaycheckDate
        ));
    }

    private static async Task<IResult> UpdateSettings(UpdateSettingsRequest request, FinanceDbContext db)
    {
        // Validate enum
        if (!Enum.TryParse<PayFrequency>(request.PayFrequency, true, out var payFrequency))
            return Results.BadRequest("Invalid pay frequency");

        // Validate amounts
        if (request.PaycheckAmount <= 0)
            return Results.BadRequest("Paycheck amount must be greater than 0");

        if (request.SafetyBuffer < 0)
            return Results.BadRequest("Safety buffer cannot be negative");

        // Deactivate all existing settings (singleton pattern)
        var existingSettings = await db.UserSettings
            .Where(s => s.IsActive)
            .ToListAsync();

        foreach (var setting in existingSettings)
        {
            setting.IsActive = false;
        }

        // Create new active settings
        var newSettings = new UserSettingsEntity
        {
            PayFrequency = payFrequency,
            PaycheckAmount = request.PaycheckAmount,
            SafetyBuffer = request.SafetyBuffer,
            NextPaycheckDate = request.NextPaycheckDate,
            IsActive = true
        };

        db.UserSettings.Add(newSettings);
        await db.SaveChangesAsync();

        return Results.Ok(new SettingsDto(
            PayFrequency: newSettings.PayFrequency.ToString(),
            PaycheckAmount: newSettings.PaycheckAmount,
            SafetyBuffer: newSettings.SafetyBuffer,
            NextPaycheckDate: newSettings.NextPaycheckDate
        ));
    }
}

// DTOs
public record SettingsDto(
    string PayFrequency,
    decimal PaycheckAmount,
    decimal SafetyBuffer,
    DateTime? NextPaycheckDate
);

public record UpdateSettingsRequest(
    string PayFrequency,
    decimal PaycheckAmount,
    decimal SafetyBuffer,
    DateTime? NextPaycheckDate
);
