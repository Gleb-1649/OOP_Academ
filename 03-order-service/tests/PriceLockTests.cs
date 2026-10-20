using OrderDesk;
using Xunit;

namespace OrderDesk.Checks;

public sealed class PriceLockTests
{
    [Fact]
    public void SendingFixesDiscountAndDeliveryCharge()
    {
        var discount = new Discount { Amount = 20m };
        var shipping = new Shipping { Fee = 40m };
        CustomerOrder order = Create(discount, shipping);
        PriceBreakdown quote = order.CalculatePrice();
        order.StartDelivery();
        discount.Amount = -1m;
        shipping.Fee = -1m;
        Assert.Equal(quote, order.CalculatePrice());
        order.CompleteDelivery();
        Assert.Equal(quote, order.CalculatePrice());
    }

    [Fact]
    public void SendingWithInvalidTariffKeepsOrderPreparing()
    {
        var shipping = new Shipping { Fee = -1m };
        CustomerOrder order = Create(new Discount(), shipping);
        Assert.Throws<InvalidOperationException>(order.StartDelivery);
        Assert.Equal(OrderStatus.Preparing, order.Status);
        shipping.Fee = 40m;
        order.StartDelivery();
        Assert.Equal(OrderStatus.InTransit, order.Status);
    }

    private static CustomerOrder Create(IDiscountStrategy discount, IDeliveryOption shipping)
    {
        var menu = new DeliveryMenu();
        menu.Add(new Dish(1, "Ужин", 200m));
        return new CheckoutBuilder(menu, 1).ForCustomer("Ольга").AddDish(1)
            .WithDiscount(discount).WithDelivery(shipping).Build();
    }

    private sealed class Discount : IDiscountStrategy
    {
        public decimal Amount { get; set; }
        public decimal GetDiscount(decimal subtotal) => Amount;
    }

    private sealed class Shipping : IDeliveryOption
    {
        public decimal Fee { get; set; }
        public string Description => "Курьер";
        public bool HasSpecialConditions => false;
        public decimal GetFee() => Fee;
    }
}
