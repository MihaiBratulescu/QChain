namespace Samples.OnlineShop.DatabaseModels;

public class Account
{
    public int AccountId { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedDate { get; set; }

    public AccountProfile Profile { get; set; } = new AccountProfile();
}
