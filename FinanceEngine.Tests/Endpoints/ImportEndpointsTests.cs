using System.Net;
using System.Net.Http.Json;
using System.Text;
using FinanceEngine.Data;
using FinanceEngine.Data.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FinanceEngine.Tests.Endpoints;

public class ImportEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ImportEndpointsTests(WebApplicationFactory<Program> factory)
    {
        var databaseName = "TestDatabase_Import_" + Guid.NewGuid();
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll(typeof(DbContextOptions<FinanceDbContext>));
                services.AddDbContext<FinanceDbContext>(options =>
                {
                    options.UseInMemoryDatabase(databaseName);
                });
            });
        });
    }

    private static string CreateCsvBase64(string csvContent)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(csvContent));
    }

    private async Task SeedTestAccount(FinanceDbContext db)
    {
        if (!db.Accounts.Any(a => a.Name == "Test Checking"))
        {
            db.Accounts.Add(new AccountEntity
            {
                Name = "Test Checking",
                Type = AccountType.Cash,
                InitialBalance = 1000m,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
    }

    #region Preview Tests

    [Fact]
    public async Task PreviewImport_ValidCsv_ReturnsPreviewWithDetectedMapping()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestAccount(db);

        var csv = "Date,Description,Amount\n01/15/2025,Coffee Shop,-5.50\n01/16/2025,Paycheck,2500.00";
        var request = new
        {
            fileName = "transactions.csv",
            fileContent = CreateCsvBase64(csv),
            accountId = 1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(result);
        Assert.Equal(3, result.Headers.Count);
        Assert.Equal(2, result.TotalRows);
        Assert.NotNull(result.DetectedMapping);
        Assert.Equal(0, result.DetectedMapping.DateColumn);
        Assert.Equal(1, result.DetectedMapping.DescriptionColumn);
        Assert.Equal(2, result.DetectedMapping.AmountColumn);
    }

    [Fact]
    public async Task PreviewImport_CsvWithDifferentDateFormat_DetectsFormat()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestAccount(db);

        var csv = "Transaction Date,Memo,Sum\n2025-01-15,Grocery Store,-45.00\n2025-01-16,ATM Withdrawal,-100.00";
        var request = new
        {
            fileName = "bank_export.csv",
            fileContent = CreateCsvBase64(csv)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.DetectedMapping);
        Assert.Contains("yyyy-MM-dd", result.DetectedMapping.DateFormat);
    }

    [Fact]
    public async Task PreviewImport_CsvWithDebitCredit_DetectsColumns()
    {
        // Arrange
        var client = _factory.CreateClient();

        var csv = "Date,Description,Debit,Credit\n01/15/2025,Coffee Shop,5.50,\n01/16/2025,Paycheck,,2500.00";
        var request = new
        {
            fileName = "transactions.csv",
            fileContent = CreateCsvBase64(csv)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<PreviewResponse>();
        Assert.NotNull(result);
        Assert.NotNull(result.DetectedMapping);
        Assert.Equal(2, result.DetectedMapping.DebitColumn);
        Assert.Equal(3, result.DetectedMapping.CreditColumn);
    }

    [Fact]
    public async Task PreviewImport_EmptyFile_ReturnsError()
    {
        // Arrange
        var client = _factory.CreateClient();

        var csv = "Date,Description,Amount";  // Only headers, no data
        var request = new
        {
            fileName = "empty.csv",
            fileContent = CreateCsvBase64(csv)
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PreviewImport_InvalidFileType_ReturnsError()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new
        {
            fileName = "data.pdf",
            fileContent = CreateCsvBase64("dummy content")
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/preview", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Commit Tests

    [Fact]
    public async Task CommitImport_ValidSession_CreatesTransactions()
    {
        // Arrange
        var client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await SeedTestAccount(db);

        var account = db.Accounts.First(a => a.Name == "Test Checking");

        // First, preview the import
        var csv = "Date,Description,Amount\n01/20/2025,Test Import 1,-10.00\n01/21/2025,Test Import 2,50.00";
        var previewRequest = new
        {
            fileName = "transactions.csv",
            fileContent = CreateCsvBase64(csv),
            accountId = account.Id
        };

        var previewResponse = await client.PostAsJsonAsync("/api/import/preview", previewRequest);
        var preview = await previewResponse.Content.ReadFromJsonAsync<PreviewResponse>();

        // Then commit
        var commitRequest = new
        {
            sessionId = preview!.SessionId,
            accountId = account.Id,
            mapping = preview.DetectedMapping
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/commit", commitRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<CommitResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.ErrorCount);
    }

    [Fact]
    public async Task CommitImport_ExpiredSession_ReturnsError()
    {
        // Arrange
        var client = _factory.CreateClient();

        var request = new
        {
            sessionId = "non-existent-session-id",
            accountId = 1,
            mapping = new
            {
                dateColumn = 0,
                descriptionColumn = 1,
                amountColumn = 2,
                dateFormat = "MM/dd/yyyy",
                hasHeaderRow = true
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/import/commit", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    // Response DTOs for deserialization
    private class PreviewResponse
    {
        public string SessionId { get; set; } = "";
        public List<string> Headers { get; set; } = new();
        public List<List<string>> SampleRows { get; set; } = new();
        public int TotalRows { get; set; }
        public MappingDto? DetectedMapping { get; set; }
        public List<PreviewRowDto> PreviewTransactions { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    private class MappingDto
    {
        public int DateColumn { get; set; }
        public int DescriptionColumn { get; set; }
        public int AmountColumn { get; set; }
        public int? DebitColumn { get; set; }
        public int? CreditColumn { get; set; }
        public int? CategoryColumn { get; set; }
        public string DateFormat { get; set; } = "";
        public bool HasHeaderRow { get; set; }
    }

    private class PreviewRowDto
    {
        public int RowNumber { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; } = "";
        public decimal Amount { get; set; }
        public bool IsDuplicate { get; set; }
        public bool IsValid { get; set; }
        public bool Selected { get; set; }
    }

    private class CommitResponse
    {
        public int ImportedCount { get; set; }
        public int SkippedCount { get; set; }
        public int DuplicateCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
