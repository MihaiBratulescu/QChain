namespace Samples.OnlineShop.DatabaseModels;

public class Transaction
{
    public int TransactionId { get; set; }
    public int OrderId { get; set; }

    public TransactionStatus Status { get; set; }

    public DateTime CreatedDate { get; set; }
}
