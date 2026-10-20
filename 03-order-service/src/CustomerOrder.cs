namespace OrderDesk;

public sealed class CustomerOrder
{
    private readonly Dictionary<int, OrderLine> lines;
    private readonly IDiscountStrategy discount;
    private readonly IDeliveryOption delivery;
    private readonly decimal taxPercent;
    private IOrderState state = new PreparingState();
    private PriceBreakdown? lockedPrice;

    public int Id { get; }
    public string Customer { get; }
    public string Preferences { get; }
    public string DeliveryDescription => delivery.Description;
    public OrderStatus Status => state.Status;
    public OrderKind Kind => delivery.HasSpecialConditions || Preferences.Length > 0
        ? OrderKind.Special : OrderKind.Standard;
    public IReadOnlyList<OrderLine> Lines => lines.Values.OrderBy(line => line.Dish.Id).ToArray();

    internal CustomerOrder(int id, string customer, string preferences,
        IEnumerable<OrderLine> lines, IDiscountStrategy discount,
        IDeliveryOption delivery, decimal taxPercent)
    {
        Id = id;
        Customer = customer;
        Preferences = preferences;
        this.lines = lines.ToDictionary(line => line.Dish.Id);
        this.discount = discount;
        this.delivery = delivery;
        this.taxPercent = taxPercent;
    }

    internal void AddDish(Dish dish, int quantity)
    {
        state.EnsureEditable();
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        int totalQuantity = lines.TryGetValue(dish.Id, out var old)
            ? checked(old.Quantity + quantity) : quantity;
        lines[dish.Id] = new OrderLine(dish, totalQuantity);
    }

    internal void RemoveDish(int dishId)
    {
        state.EnsureEditable();
        if (!lines.ContainsKey(dishId)) throw new KeyNotFoundException("Блюдо не включено в заказ.");
        if (lines.Count == 1) throw new InvalidOperationException("Заказ не может остаться пустым.");
        lines.Remove(dishId);
    }

    public void StartDelivery() => state.StartDelivery(this);
    public void CompleteDelivery() => state.CompleteDelivery(this);
    internal void SetState(IOrderState nextState) => state = nextState;
    internal void LockPrice() => lockedPrice = CalculatePrice();

    public PriceBreakdown CalculatePrice()
    {
        if (lockedPrice is not null) return lockedPrice;
        decimal subtotal = Round(lines.Values.Sum(line => line.Total));
        decimal rawDiscount = discount.GetDiscount(subtotal);
        if (rawDiscount < 0 || rawDiscount > subtotal)
            throw new InvalidOperationException("Скидка должна быть от нуля до стоимости блюд.");
        decimal discountAmount = Round(rawDiscount);
        decimal afterDiscount = subtotal - discountAmount;
        decimal tax = Round(afterDiscount * taxPercent / 100m);
        decimal rawFee = delivery.GetFee();
        if (rawFee < 0) throw new InvalidOperationException("Плата за доставку не может быть отрицательной.");
        decimal fee = Round(rawFee);
        decimal total = Round(afterDiscount + tax + fee);
        return new PriceBreakdown(subtotal, discountAmount, tax, fee, total);
    }

    private static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);
}
