namespace Samples.OnlineShop.DatabaseModels;

public class Order
{
    public int OrderId { get; set; }
    
    public int AccountId { get; set; }

    public decimal Total { get; set; }
    public CurrencyType CurrencyId { get; set; }

    public DateTime CreatedDate { get; set; }
}
