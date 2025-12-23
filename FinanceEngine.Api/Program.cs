using FinanceEngine.Api.Endpoints;
using FinanceEngine.Api.Services;
using FinanceEngine.Data;
using FinanceEngine.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();

if (!builder.Environment.IsEnvironment("Testing"))
{
    // Configure EF Core with SQLite
    builder.Services.AddDbContext<FinanceDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=finance.db"));
}

// Register repositories
builder.Services.AddScoped<IAccountRepository, AccountRepository>();

// Register services
builder.Services.AddScoped<IAccountService, AccountService>();

// Configure CORS for Angular dev server
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors("AllowAngular");
}

app.UseHttpsRedirection();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    db.Database.EnsureCreated();
}

// Map endpoint groups
app.MapGroup("/api/accounts").MapAccountEndpoints();
app.MapGroup("/api/events").MapEventEndpoints();
app.MapGroup("/api/calculators").MapCalculatorEndpoints();
app.MapGroup("/api/settings").MapSettingsEndpoints();

// COMPLETED: One-time migration to fix APR percentage values (ran on 2025-12-21)
// Endpoint commented out to prevent accidental re-execution
// app.MapPost("/api/migrate/fix-apr-percentages", async (FinanceDbContext db) =>
// {
//     var accounts = await db.Accounts.Where(a => a.IsActive).ToListAsync();
//     var updated = 0;
//
//     foreach (var account in accounts)
//     {
//         bool needsUpdate = false;
//
//         // Fix AnnualPercentageRate if it's >= 1 (stored as percentage instead of decimal)
//         if (account.AnnualPercentageRate.HasValue && account.AnnualPercentageRate.Value >= 1)
//         {
//             account.AnnualPercentageRate = account.AnnualPercentageRate.Value / 100;
//             needsUpdate = true;
//         }
//
//         // Fix PromotionalAnnualPercentageRate
//         if (account.PromotionalAnnualPercentageRate.HasValue && account.PromotionalAnnualPercentageRate.Value >= 1)
//         {
//             account.PromotionalAnnualPercentageRate = account.PromotionalAnnualPercentageRate.Value / 100;
//             needsUpdate = true;
//         }
//
//         // Fix BalanceTransferFeePercentage
//         if (account.BalanceTransferFeePercentage.HasValue && account.BalanceTransferFeePercentage.Value >= 1)
//         {
//             account.BalanceTransferFeePercentage = account.BalanceTransferFeePercentage.Value / 100;
//             needsUpdate = true;
//         }
//
//         if (needsUpdate)
//         {
//             updated++;
//         }
//     }
//
//     if (updated > 0)
//     {
//         await db.SaveChangesAsync();
//     }
//
//     return Results.Ok(new { message = $"Fixed {updated} account(s)", accountsChecked = accounts.Count });
// });

app.Run();

// Make Program accessible to tests
public partial class Program { }
