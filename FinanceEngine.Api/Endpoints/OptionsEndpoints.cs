using FinanceEngine.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceEngine.Api.Endpoints;

public static class OptionsEndpoints
{
    public static RouteGroupBuilder MapOptionsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/", GetOptions);
        group.MapGet("/{id}", GetOptionById);
        group.MapPost("/", CreateOption);
        group.MapPut("/{id}", UpdateOption);
        group.MapDelete("/{id}", DeleteOption);

        return group;
    }

    private static async Task<IResult> GetOptions(FinanceDbContext db)
    {
        var options = await db.OptionContracts.ToListAsync();
        return Results.Ok(options);
    }

    private static async Task<IResult> GetOptionById(Guid id, FinanceDbContext db)
    {
        var option = await db.OptionContracts.FindAsync(id);
        return option is not null ? Results.Ok(option) : Results.NotFound();
    }

    private static async Task<IResult> CreateOption(OptionContract option, FinanceDbContext db)
    {
        db.OptionContracts.Add(option);
        await db.SaveChangesAsync();
        return Results.Created($"/api/options/{option.Id}", option);
    }

    private static async Task<IResult> UpdateOption(Guid id, OptionContract inputOption, FinanceDbContext db)
    {
        var option = await db.OptionContracts.FindAsync(id);
        if (option is null) return Results.NotFound();

        option.TickerSymbol = inputOption.TickerSymbol;
        option.StrikePrice = inputOption.StrikePrice;
        option.ExpirationDate = inputOption.ExpirationDate;
        option.OptionType = inputOption.OptionType;
        option.Position = inputOption.Position;
        option.Premium = inputOption.Premium;
        option.Quantity = inputOption.Quantity;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteOption(Guid id, FinanceDbContext db)
    {
        if (await db.OptionContracts.FindAsync(id) is OptionContract option)
        {
            db.OptionContracts.Remove(option);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }
        return Results.NotFound();
    }
}
