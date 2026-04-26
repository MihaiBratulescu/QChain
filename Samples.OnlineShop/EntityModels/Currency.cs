namespace Samples.OnlineShop.DatabaseModels;

public class Currency
{
    public CurrencyType CurrencyId { get; set; }
    public string Symbol { get; set; } = null!;
}