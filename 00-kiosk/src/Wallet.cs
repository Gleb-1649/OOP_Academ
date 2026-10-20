namespace Kiosk;

public sealed class Wallet
{
    private readonly List<int> coins = [];
    public int Balance { get; private set; }
    public IReadOnlyList<int> Coins => coins.ToArray();

    public void Insert(int coin)
    {
        if (coin is not (1 or 2 or 5 or 10))
            throw new ArgumentException("Принимаются монеты 1, 2, 5 и 10 рублей.", nameof(coin));
        int newBalance = checked(Balance + coin);
        coins.Add(coin);
        Balance = newBalance;
    }

    public IReadOnlyList<int> ReturnAll()
    {
        int[] result = coins.ToArray();
        coins.Clear();
        Balance = 0;
        return result;
    }
}

public sealed record Purchase(string ProductName, int Change, IReadOnlyList<int> ChangeCoins);
