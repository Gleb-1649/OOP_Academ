namespace OrderDesk;
public interface IDiscountStrategy
{
    decimal GetDiscount(decimal subtotal);
}

public sealed class NoDiscount : IDiscountStrategy
{
    public decimal GetDiscount(decimal subtotal) => 0m;
}

public sealed class PercentageDiscount : IDiscountStrategy
{
    public decimal Percent { get; }

    public PercentageDiscount(decimal percent)
    {
        if (percent < 0 || percent > 100) throw new ArgumentOutOfRangeException(nameof(percent));
        Percent = percent;
    }

    public decimal GetDiscount(decimal subtotal) => subtotal * Percent / 100m;
}

public interface IDeliveryOption
{
    string Description { get; }
    bool HasSpecialConditions { get; }
    decimal GetFee();
}

public sealed class StandardDelivery : IDeliveryOption
{
    private readonly decimal fee;
    public string Description => "Обычная доставка";
    public bool HasSpecialConditions => false;

    public StandardDelivery(decimal fee = 90m)
    {
        if (fee < 0) throw new ArgumentOutOfRangeException(nameof(fee));
        this.fee = fee;
    }

    public decimal GetFee() => fee;
}
public sealed class ExpressDeliveryDecorator : IDeliveryOption
{
    private readonly IDeliveryOption inner;
    private readonly decimal surcharge;
    public string Description => inner.Description + ", приоритетная доставка";
    public bool HasSpecialConditions => true;

    public ExpressDeliveryDecorator(IDeliveryOption inner, decimal surcharge = 80m)
    {
        ArgumentNullException.ThrowIfNull(inner);
        if (surcharge < 0) throw new ArgumentOutOfRangeException(nameof(surcharge));
        this.inner = inner;
        this.surcharge = surcharge;
    }

    public decimal GetFee() => inner.GetFee() + surcharge;
}

public sealed record PriceBreakdown(decimal Subtotal, decimal Discount,
    decimal Tax, decimal DeliveryFee, decimal Total);
