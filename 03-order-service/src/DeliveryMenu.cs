namespace OrderDesk;

public sealed class Dish
{
    public int Id { get; }
    public string Name { get; }
    public decimal Price { get; }

    public Dish(int id, string name, decimal price)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Название блюда обязательно.");
        decimal rounded = Math.Round(price, 2, MidpointRounding.AwayFromZero);
        if (rounded <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        Id = id;
        Name = name.Trim();
        Price = rounded;
    }
}

public sealed class DeliveryMenu
{
    private readonly Dictionary<int, Dish> dishes = [];
    public IReadOnlyList<Dish> Dishes => dishes.Values.OrderBy(d => d.Id).ToArray();

    public void Add(Dish dish)
    {
        ArgumentNullException.ThrowIfNull(dish);
        if (!dishes.TryAdd(dish.Id, dish)) throw new ArgumentException("Номер блюда уже занят.");
    }

    public Dish Find(int dishId) => dishes.TryGetValue(dishId, out var dish)
        ? dish : throw new KeyNotFoundException("Блюдо отсутствует в меню.");
}

public sealed class OrderLine
{
    public Dish Dish { get; }
    public int Quantity { get; }
    public decimal Total => Dish.Price * Quantity;

    public OrderLine(Dish dish, int quantity)
    {
        ArgumentNullException.ThrowIfNull(dish);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        Dish = dish;
        Quantity = quantity;
    }
}
