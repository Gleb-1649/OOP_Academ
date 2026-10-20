using OrderDesk;
using System.Globalization;
using Xunit;

namespace OrderDesk.Checks;

public class DeliveryTests
{
    [Fact]
    public void FacadeCreatesOrderFromMenuAndStoresIt()
    {
        var service = CreateService();
        CustomerOrder order = service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 2, [2] = 1 });
        Assert.Equal(2, service.GetMenu().Count);
        Assert.Same(order, service.GetOrder(101));
        Assert.Equal("Мария", order.Customer);
        Assert.Equal(2, order.Lines.Count);
        Assert.Equal(2, order.Lines[0].Quantity);
        Assert.Equal("Суп-пюре", order.Lines[0].Dish.Name);
        Assert.Equal(OrderStatus.Preparing, order.Status);
    }

    [Fact]
    public void AddingSameDishCombinesQuantityAndRemovingDeletesLine()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        service.AddDishToOrder(101, 1, 2);
        service.AddDishToOrder(101, 2);
        Assert.Equal(3, service.GetOrder(101).Lines[0].Quantity);
        service.RemoveDishFromOrder(101, 1);
        Assert.Equal(2, Assert.Single(service.GetOrder(101).Lines).Dish.Id);
    }

    [Fact]
    public void LastDishCannotBeRemoved()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Throws<InvalidOperationException>(() => service.RemoveDishFromOrder(101, 1));
        Assert.Throws<KeyNotFoundException>(() => service.RemoveDishFromOrder(101, 2));
        Assert.Single(service.GetOrder(101).Lines);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void InvalidQuantityDoesNotCreateOrChangeOrder(int quantity)
    {
        var service = CreateService();
        Assert.Throws<ArgumentOutOfRangeException>(() => service.PlaceOrder(101, "Мария",
            new Dictionary<int, int> { [1] = quantity }));
        Assert.Empty(service.GetOrders());
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => service.AddDishToOrder(101, 1, quantity));
        Assert.Equal(1, Assert.Single(service.GetOrder(101).Lines).Quantity);
    }

    [Fact]
    public void UnknownMenuDishAndUnknownOrderAreRejected()
    {
        var service = CreateService();
        Assert.Throws<KeyNotFoundException>(() => service.PlaceOrder(101, "Мария",
            new Dictionary<int, int> { [99] = 1 }));
        Assert.Empty(service.GetOrders());
        Assert.Throws<KeyNotFoundException>(() => service.Send(99));
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Throws<KeyNotFoundException>(() => service.AddDishToOrder(101, 99));
        Assert.Single(service.GetOrder(101).Lines);
    }

    [Fact]
    public void BuilderRejectsIncompleteOrders()
    {
        DeliveryMenu menu = CreateMenu();
        Assert.Throws<InvalidOperationException>(() => new CheckoutBuilder(menu, 1).AddDish(1).Build());
        Assert.Throws<InvalidOperationException>(() => new CheckoutBuilder(menu, 1).ForCustomer("Мария").Build());
        Assert.Throws<ArgumentException>(() => new CheckoutBuilder(menu, 1).ForCustomer(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckoutBuilder(menu, 0));
    }

    [Fact]
    public void BuilderReuseDoesNotChangeEarlierResult()
    {
        var builder = new CheckoutBuilder(CreateMenu(), 1).ForCustomer("Мария").AddDish(1, 2);
        CustomerOrder first = builder.Build();
        CustomerOrder second = builder.AddDish(1, 3).AddDish(2).WithPreferences("Без лука").Build();
        Assert.Equal(2, Assert.Single(first.Lines).Quantity);
        Assert.Equal("", first.Preferences);
        Assert.Equal(2, second.Lines.Count);
        Assert.Equal(5, second.Lines[0].Quantity);
        Assert.Equal("Без лука", second.Preferences);
    }

    [Fact]
    public void DuplicateOrderIdPreservesExistingOrder()
    {
        var service = CreateService();
        CustomerOrder original = service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Throws<ArgumentException>(() => service.PlaceOrder(101, "Петр",
            new Dictionary<int, int> { [2] = 2 }));
        Assert.Same(original, Assert.Single(service.GetOrders()));
    }

    [Fact]
    public void StateSequenceIsPreparingThenInTransitThenDelivered()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Equal(OrderStatus.Preparing, service.GetStatus(101));
        service.Send(101);
        Assert.Equal(OrderStatus.InTransit, service.GetStatus(101));
        service.Finish(101);
        Assert.Equal(OrderStatus.Delivered, service.GetStatus(101));
    }

    [Fact]
    public void InvalidTransitionsKeepCurrentState()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        Assert.Throws<InvalidOperationException>(() => service.Finish(101));
        Assert.Equal(OrderStatus.Preparing, service.GetStatus(101));
        service.Send(101);
        Assert.Throws<InvalidOperationException>(() => service.Send(101));
        Assert.Equal(OrderStatus.InTransit, service.GetStatus(101));
        service.Finish(101);
        Assert.Throws<InvalidOperationException>(() => service.Finish(101));
        Assert.Throws<InvalidOperationException>(() => service.Send(101));
        Assert.Equal(OrderStatus.Delivered, service.GetStatus(101));
    }

    [Fact]
    public void InTransitAndDeliveredOrdersCannotBeEdited()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1, [2] = 1 });
        decimal price = service.GetPrice(101).Total;
        service.Send(101);
        Assert.Throws<InvalidOperationException>(() => service.AddDishToOrder(101, 1));
        Assert.Throws<InvalidOperationException>(() => service.RemoveDishFromOrder(101, 2));
        service.Finish(101);
        Assert.Throws<InvalidOperationException>(() => service.AddDishToOrder(101, 2));
        Assert.Throws<InvalidOperationException>(() => service.RemoveDishFromOrder(101, 1));
        Assert.Equal(2, service.GetOrder(101).Lines.Count);
        Assert.Equal(price, service.GetPrice(101).Total);
    }

    [Fact]
    public void PriceAppliesDiscountThenTaxThenDelivery()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 2, [2] = 1 },
            discount: new PercentageDiscount(10));
        PriceBreakdown price = service.GetPrice(101);
        Assert.Equal(480m, price.Subtotal);
        Assert.Equal(48m, price.Discount);
        Assert.Equal(43.20m, price.Tax);
        Assert.Equal(90m, price.DeliveryFee);
        Assert.Equal(565.20m, price.Total);
    }

    [Fact]
    public void NoDiscountStrategyAndZeroTaxKeepFoodSubtotal()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 }, taxPercent: 0);
        PriceBreakdown price = service.GetPrice(101);
        Assert.Equal(0m, price.Discount);
        Assert.Equal(0m, price.Tax);
        Assert.Equal(270m, price.Total);
    }

    [Fact]
    public void DecoratorKeepsBaseFeeAndAddsExpressSurcharge()
    {
        IDeliveryOption ordinary = new StandardDelivery(40m);
        IDeliveryOption express = new ExpressDeliveryDecorator(ordinary, 25m);
        Assert.Equal(40m, ordinary.GetFee());
        Assert.Equal(65m, express.GetFee());
        Assert.Contains(ordinary.Description, express.Description);
        Assert.True(express.HasSpecialConditions);
        var service = CreateService();
        CustomerOrder order = service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 }, express: true);
        Assert.Equal(OrderKind.Special, order.Kind);
        Assert.Equal(170m, service.GetPrice(101).DeliveryFee);
    }

    [Fact]
    public void PreferencesCreateSpecialOrderWithoutChangingDeliveryFee()
    {
        var service = CreateService();
        CustomerOrder standard = service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 });
        CustomerOrder special = service.PlaceOrder(102, "Петр", new Dictionary<int, int> { [2] = 1 },
            preferences: "  Без орехов  ");
        Assert.Equal(OrderKind.Standard, standard.Kind);
        Assert.Equal(OrderKind.Special, special.Kind);
        Assert.Equal("Без орехов", special.Preferences);
        Assert.Equal(90m, special.CalculatePrice().DeliveryFee);
    }

    [Fact]
    public void FullDiscountStillRequiresDeliveryPayment()
    {
        var service = CreateService();
        service.PlaceOrder(101, "Мария", new Dictionary<int, int> { [1] = 1 },
            discount: new PercentageDiscount(100));
        PriceBreakdown price = service.GetPrice(101);
        Assert.Equal(180m, price.Discount);
        Assert.Equal(0m, price.Tax);
        Assert.Equal(90m, price.Total);
    }

    [Fact]
    public void MoneyRoundsToTwoDigitsAwayFromZeroAtEachStage()
    {
        var menu = new DeliveryMenu();
        menu.Add(new Dish(1, "Пробное блюдо", 10.05m));
        CustomerOrder order = new CheckoutBuilder(menu, 1).ForCustomer("Мария").AddDish(1)
            .WithDiscount(new PercentageDiscount(10)).WithTaxRate(10)
            .WithDelivery(new StandardDelivery(0.105m)).Build();
        PriceBreakdown price = order.CalculatePrice();
        Assert.Equal(1.01m, price.Discount);
        Assert.Equal(0.90m, price.Tax);
        Assert.Equal(0.11m, price.DeliveryFee);
        Assert.Equal(10.05m, price.Total);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void InvalidRatesAreRejected(int percent)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PercentageDiscount(percent));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CheckoutBuilder(CreateMenu(), 1).WithTaxRate(percent));
    }

    [Fact]
    public void MenuAndDeliveryRejectInvalidValues()
    {
        DeliveryMenu menu = CreateMenu();
        Assert.Throws<ArgumentException>(() => menu.Add(new Dish(1, "Дубликат", 1m)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Dish(3, "Блюдо", 0m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new StandardDelivery(-1m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExpressDeliveryDecorator(new StandardDelivery(), -1m));
        Assert.Equal(2, menu.Dishes.Count);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("1000")]
    [InlineData("-0.001")]
    [InlineData("180.001")]
    public void InvalidCustomDiscountCannotProduceNegativeOrInflatedPrice(string amount)
    {
        CustomerOrder order = new CheckoutBuilder(CreateMenu(), 1).ForCustomer("Мария").AddDish(1)
            .WithDiscount(new FixedDiscount(decimal.Parse(amount, CultureInfo.InvariantCulture))).Build();
        Assert.Throws<InvalidOperationException>(() => order.CalculatePrice());
    }

    [Fact]
    public void FractionalNegativeDeliveryFeeIsRejectedBeforeRounding()
    {
        CustomerOrder order = new CheckoutBuilder(CreateMenu(), 1).ForCustomer("Мария").AddDish(1)
            .WithDelivery(new NegativeFeeDelivery()).Build();
        Assert.Throws<InvalidOperationException>(() => order.CalculatePrice());
    }

    private static DeliveryMenu CreateMenu()
    {
        var menu = new DeliveryMenu();
        menu.Add(new Dish(1, "Суп-пюре", 180m));
        menu.Add(new Dish(2, "Сэндвич с овощами", 120m));
        return menu;
    }

    private static DeliveryFacade CreateService() => new(CreateMenu());

    private sealed class FixedDiscount : IDiscountStrategy
    {
        private readonly decimal amount;
        public FixedDiscount(decimal amount) => this.amount = amount;
        public decimal GetDiscount(decimal subtotal) => amount;
    }

    private sealed class NegativeFeeDelivery : IDeliveryOption
    {
        public string Description => "Ошибочный тариф для теста";
        public bool HasSpecialConditions => false;
        public decimal GetFee() => -0.001m;
    }
}
