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

// Apply pending migrations (or create database for in-memory/testing)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Map endpoint groups
app.MapGroup("/api/accounts").MapAccountEndpoints();
app.MapGroup("/api/events").MapEventEndpoints();
app.MapGroup("/api/calculators").MapCalculatorEndpoints();
app.MapGroup("/api/settings").MapSettingsEndpoints();
app.MapGroup("/api/recurring-contributions").MapRecurringContributionEndpoints();

app.Run();

// Make Program accessible to tests
public partial class Program { }
