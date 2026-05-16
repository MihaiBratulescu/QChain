namespace Samples.OnlineShop.DatabaseModels;

public class AccountProfile
{
    public int AccountProfileId { get; set; }
    public int AccountId { get; set; }

    public string? Name { get; set; }
}