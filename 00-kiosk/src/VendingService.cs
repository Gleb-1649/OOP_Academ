namespace Kiosk;

public sealed class VendingService
{
    private readonly Dictionary<string, CatalogEntry> catalog = new(StringComparer.OrdinalIgnoreCase);
    private readonly Wallet wallet = new();
    public int Balance => wallet.Balance;
    public int Cash { get; private set; }
    public IReadOnlyList<CatalogEntry> Products => catalog.Values.OrderBy(p => p.Code).ToArray();

    public void AddProduct(CatalogEntry product)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!catalog.TryAdd(product.Code, product))
            throw new ArgumentException("Товар с таким кодом уже существует.");
    }

    public void Restock(string code, int count) => FindProduct(code).AddStock(count);
    public void InsertCoin(int coin) => wallet.Insert(coin);
    public IReadOnlyList<int> Cancel() => wallet.ReturnAll();

    public Purchase Buy(string code)
    {
        CatalogEntry product = FindProduct(code);
        if (product.Stock == 0) throw new InvalidOperationException("Товар закончился.");
        if (Balance < product.Price) throw new InvalidOperationException("Недостаточно денег.");
        int newCash = checked(Cash + product.Price);
        int change = Balance - product.Price;
        var changeCoins = new List<int>();
        int remainder = change;
        foreach (int coin in new[] { 10, 5, 2, 1 })
            while (remainder >= coin)
            {
                changeCoins.Add(coin);
                remainder -= coin;
            }
        product.TakeOne();
        wallet.ReturnAll();
        Cash = newCash;
        return new Purchase(product.Name, change, changeCoins);
    }

    public int CollectCash()
    {
        int result = Cash;
        Cash = 0;
        return result;
    }

    private CatalogEntry FindProduct(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("Укажите код товара.");
        return catalog.TryGetValue(code.Trim(), out var product)
            ? product : throw new KeyNotFoundException("Неизвестный код товара.");
    }
}
