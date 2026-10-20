namespace OrderDesk;
public sealed class CheckoutBuilder
{
    private readonly DeliveryMenu menu;
    private readonly int orderId;
    private readonly Dictionary<int, OrderLine> lines = [];
    private string customer = "";
    private string preferences = "";
    private decimal taxPercent = 10m;
    private IDiscountStrategy discount = new NoDiscount();
    private IDeliveryOption delivery = new StandardDelivery();

    public CheckoutBuilder(DeliveryMenu menu, int orderId)
    {
        ArgumentNullException.ThrowIfNull(menu);
        if (orderId <= 0) throw new ArgumentOutOfRangeException(nameof(orderId));
        this.menu = menu;
        this.orderId = orderId;
    }

    public CheckoutBuilder ForCustomer(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Имя клиента обязательно.");
        customer = name.Trim();
        return this;
    }

    public CheckoutBuilder AddDish(int dishId, int quantity = 1)
    {
        Dish dish = menu.Find(dishId);
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        int totalQuantity = lines.TryGetValue(dishId, out var old)
            ? checked(old.Quantity + quantity) : quantity;
        lines[dishId] = new OrderLine(dish, totalQuantity);
        return this;
    }

    public CheckoutBuilder WithPreferences(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        preferences = text.Trim();
        return this;
    }

    public CheckoutBuilder WithDiscount(IDiscountStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(strategy);
        discount = strategy;
        return this;
    }

    public CheckoutBuilder WithDelivery(IDeliveryOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        delivery = option;
        return this;
    }

    public CheckoutBuilder WithTaxRate(decimal percent)
    {
        if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        taxPercent = percent;
        return this;
    }

    public CustomerOrder Build()
    {
        if (customer.Length == 0) throw new InvalidOperationException("Сначала укажите клиента.");
        if (lines.Count == 0) throw new InvalidOperationException("Добавьте хотя бы одно блюдо.");
        return new CustomerOrder(orderId, customer, preferences, lines.Values, discount, delivery, taxPercent);
    }
}
