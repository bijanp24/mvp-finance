using System;

namespace FinanceEngine.Data
{
    public class OptionContract
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TickerSymbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string OptionType { get; set; } = "Call"; // "Call" or "Put"
        public string Position { get; set; } = "Long"; // "Long" or "Short"
        public decimal Premium { get; set; }
        public int Quantity { get; set; }
    }
}
