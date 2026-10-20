namespace OrderDesk;
public sealed class DeliveryFacade
{
    private readonly DeliveryMenu menu;
    private readonly Dictionary<int, CustomerOrder> orders = [];

    public DeliveryFacade(DeliveryMenu menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        this.menu = menu;
    }

    public IReadOnlyList<Dish> GetMenu() => menu.Dishes;
    public IReadOnlyList<CustomerOrder> GetOrders() => orders.Values.OrderBy(order => order.Id).ToArray();

    public CustomerOrder PlaceOrder(int orderId, string customer, IReadOnlyDictionary<int, int> selection,
        IDiscountStrategy? discount = null, bool express = false, string preferences = "",
        decimal taxPercent = 10m)
    {
        ArgumentNullException.ThrowIfNull(selection);
        if (orders.ContainsKey(orderId)) throw new ArgumentException("Номер заказа уже занят.");
        IDeliveryOption delivery = new StandardDelivery();
        if (express) delivery = new ExpressDeliveryDecorator(delivery);
        var builder = new CheckoutBuilder(menu, orderId)
            .ForCustomer(customer)
            .WithPreferences(preferences)
            .WithDiscount(discount ?? new NoDiscount())
            .WithTaxRate(taxPercent)
            .WithDelivery(delivery);
        foreach (var pair in selection) builder.AddDish(pair.Key, pair.Value);
        CustomerOrder order = builder.Build();
        orders.Add(orderId, order);
        return order;
    }

    public CustomerOrder GetOrder(int orderId) => orders.TryGetValue(orderId, out var order)
        ? order : throw new KeyNotFoundException("Заказ не найден.");

    public void AddDishToOrder(int orderId, int dishId, int quantity = 1) =>
        GetOrder(orderId).AddDish(menu.Find(dishId), quantity);

    public void RemoveDishFromOrder(int orderId, int dishId) => GetOrder(orderId).RemoveDish(dishId);
    public void Send(int orderId) => GetOrder(orderId).StartDelivery();
    public void Finish(int orderId) => GetOrder(orderId).CompleteDelivery();
    public PriceBreakdown GetPrice(int orderId) => GetOrder(orderId).CalculatePrice();
    public OrderStatus GetStatus(int orderId) => GetOrder(orderId).Status;
}
