using System;

namespace FinanceEngine.Data
{
    public class OptionTransaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OptionContractId { get; set; }
        public OptionContract Contract { get; set; } = null!;
        public DateTime TransactionDate { get; set; }
        public string Action { get; set; } = "BuyToOpen"; // BuyToOpen, SellToClose, etc.
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
