namespace Kiosk;

public sealed class CatalogEntry
{
    public string Code { get; }
    public string Name { get; }
    public int Price { get; }
    public int Stock { get; private set; }

    public CatalogEntry(string code, string name, int price, int stock)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Код и название обязательны.");
        if (price <= 0 || stock < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Цена положительна, остаток неотрицателен.");
        Code = code.Trim();
        Name = name.Trim();
        Price = price;
        Stock = stock;
    }

    public void AddStock(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        Stock = checked(Stock + count);
    }

    internal void TakeOne()
    {
        if (Stock == 0) throw new InvalidOperationException("Товар закончился.");
        Stock--;
    }
}
