namespace Samples.OnlineShop.DatabaseModels;

public enum TransactionStatus
{
    Pending,
    Settled,
    Refunded,
    Failed
}

public enum CurrencyType
{
    EUR, USD, BTC, ETH
}