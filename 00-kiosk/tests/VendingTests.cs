using Kiosk;
using Xunit;

namespace Kiosk.Checks;

public class VendingTests
{
    [Fact]
    public void PurchaseReturnsChangeAndCountsOnlySaleRevenue()
    {
        var machine = CreateMachine();
        machine.InsertCoin(10);
        machine.InsertCoin(10);
        Purchase result = machine.Buy("tea");
        Assert.Equal("Чай", result.ProductName);
        Assert.Equal(3, result.Change);
        Assert.Equal(new[] { 2, 1 }, result.ChangeCoins.ToArray());
        Assert.Equal(0, machine.Balance);
        Assert.Equal(17, machine.Cash);
        Assert.Equal(1, Assert.Single(machine.Products).Stock);
    }

    [Fact]
    public void ExactPaymentHasNoChange()
    {
        var machine = CreateMachine();
        machine.InsertCoin(10);
        machine.InsertCoin(5);
        machine.InsertCoin(2);
        Assert.Empty(machine.Buy("tea").ChangeCoins);
    }

    [Fact]
    public void CancellationReturnsOriginalCoins()
    {
        var machine = CreateMachine();
        machine.InsertCoin(5);
        machine.InsertCoin(2);
        machine.InsertCoin(5);
        Assert.Equal(new[] { 5, 2, 5 }, machine.Cancel().ToArray());
        Assert.Equal(0, machine.Balance);
        Assert.Equal(0, machine.Cash);
        Assert.Empty(machine.Cancel());
    }

    [Fact]
    public void InsufficientPaymentDoesNotConsumeMoneyOrStock()
    {
        var machine = CreateMachine();
        machine.InsertCoin(10);
        Assert.Throws<InvalidOperationException>(() => machine.Buy("tea"));
        Assert.Equal(10, machine.Balance);
        Assert.Equal(2, Assert.Single(machine.Products).Stock);
        Assert.Equal(0, machine.Cash);
    }

    [Fact]
    public void UnknownAndEmptyProductsDoNotConsumeMoney()
    {
        var machine = new VendingService();
        machine.AddProduct(new CatalogEntry("empty", "Нет в наличии", 1, 0));
        machine.InsertCoin(5);
        Assert.Throws<KeyNotFoundException>(() => machine.Buy("missing"));
        Assert.Throws<InvalidOperationException>(() => machine.Buy("empty"));
        Assert.Equal(5, machine.Balance);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(50)]
    public void UnsupportedCoinDoesNotChangeBalance(int coin)
    {
        var machine = CreateMachine();
        Assert.Throws<ArgumentException>(() => machine.InsertCoin(coin));
        Assert.Equal(0, machine.Balance);
    }

    [Fact]
    public void AdministratorCanAddRestockAndCollect()
    {
        var machine = CreateMachine();
        machine.Restock("TEA", 3);
        Assert.Equal(5, Assert.Single(machine.Products).Stock);
        machine.AddProduct(new CatalogEntry("water", "Вода", 10, 1));
        machine.InsertCoin(10);
        machine.Buy("water");
        Assert.Equal(10, machine.CollectCash());
        Assert.Equal(0, machine.CollectCash());
    }

    [Fact]
    public void CollectionLeavesCurrentCustomerMoneyUntouched()
    {
        var machine = CreateMachine();
        machine.InsertCoin(5);
        Assert.Equal(0, machine.CollectCash());
        Assert.Equal(new[] { 5 }, machine.Cancel().ToArray());
    }

    [Fact]
    public void DuplicateCodeAndInvalidRestockAreRejected()
    {
        var machine = CreateMachine();
        Assert.Throws<ArgumentException>(() => machine.AddProduct(new CatalogEntry("TEA", "Другой чай", 1, 1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => machine.Restock("tea", 0));
        Assert.Throws<KeyNotFoundException>(() => machine.Restock("missing", 1));
        Assert.Equal(2, Assert.Single(machine.Products).Stock);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, -1)]
    public void InvalidProductValuesAreRejected(int price, int stock) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new CatalogEntry("a", "Товар", price, stock));

    private static VendingService CreateMachine()
    {
        var machine = new VendingService();
        machine.AddProduct(new CatalogEntry("tea", "Чай", 17, 2));
        return machine;
    }
}
