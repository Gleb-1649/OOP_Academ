using Backpack;
using Xunit;

namespace Backpack.Checks;

public sealed class ItemLifetimeTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void HealingRemovesPotionAfterAnEarlierFailedAttempt(bool directCall)
    {
        var stats = new PlayerStats(100);
        var backpack = new PlayerInventory(stats);
        var remedy = new HealthPotion(8, "Настой", 20);
        var quest = new QuestObject(9, "Письмо", "Почта");
        backpack.Add(remedy);
        backpack.Add(quest);
        Assert.Throws<InvalidOperationException>(() => remedy.Use(stats));
        Assert.False(remedy.IsUsed);
        Assert.Equal(2, backpack.Items.Count);

        stats.TakeDamage(50);
        if (directCall) remedy.Use(stats);
        else backpack.Use(8);

        Assert.Equal(70, stats.Health);
        Assert.True(remedy.IsUsed);
        Assert.Same(quest, Assert.Single(backpack.Items));
        Assert.Single(backpack.DescribeContents());
        Assert.Throws<KeyNotFoundException>(() => backpack.GetItem(8));
        Assert.Throws<InvalidOperationException>(() => backpack.Add(remedy));
        Assert.Throws<InvalidOperationException>(() => remedy.Use(stats));
        Assert.Equal(70, stats.Health);
    }

    [Fact]
    public void LoosePotionDoesNotRequireABackpack()
    {
        var stats = new PlayerStats(100);
        var remedy = new HealthPotion(8, "Настой", 20);
        stats.TakeDamage(50);
        remedy.Use(stats);
        Assert.Equal(70, stats.Health);
        Assert.True(remedy.IsUsed);
        Assert.Throws<InvalidOperationException>(() => remedy.Use(stats));
    }

    [Fact]
    public void PotionBelongingToAnotherBagCannotHealPlayer()
    {
        var owner = new PlayerStats(100);
        var stranger = new PlayerStats(100);
        var bag = new PlayerInventory(owner);
        var potion = new HealthPotion(1, "Настой", 20);
        bag.Add(potion);
        stranger.TakeDamage(50);
        Assert.Throws<InvalidOperationException>(() => potion.Use(stranger));
        Assert.Equal(50, stranger.Health);
        Assert.False(potion.IsUsed);
    }

    [Fact]
    public void UsedPotionCannotBeReinsertedOrUsedDirectlyAgain()
    {
        var player = new PlayerStats(100);
        var bag = new PlayerInventory(player);
        var potion = new HealthPotion(1, "Настой", 20);
        player.TakeDamage(70);
        bag.Add(potion);
        bag.Use(1);
        Assert.Equal(50, player.Health);
        Assert.Throws<InvalidOperationException>(() => bag.Add(potion));
        Assert.Throws<InvalidOperationException>(() => potion.Use(player));
        Assert.Equal(50, player.Health);
        Assert.Empty(bag.Items);
    }

    [Fact]
    public void FailedHealingDoesNotConsumePotion()
    {
        var player = new PlayerStats(100);
        var bag = new PlayerInventory(player);
        var potion = new HealthPotion(1, "Настой", 20);
        bag.Add(potion);
        Assert.Throws<InvalidOperationException>(() => bag.Use(1));
        Assert.Same(potion, Assert.Single(bag.Items));
        player.TakeDamage(30);
        bag.Use(1);
        Assert.Equal(90, player.Health);
        Assert.Empty(bag.Items);
    }

    [Fact]
    public void EquipmentHasOneInventoryUntilItIsRemoved()
    {
        var a = new PlayerInventory(new PlayerStats(100));
        var b = new PlayerInventory(new PlayerStats(100));
        var sword = new Sword(1, "Сабля", 7);
        a.Add(sword);
        Assert.Throws<InvalidOperationException>(() => b.Add(sword));
        Assert.Empty(b.Items);
        a.Equip(1);
        Assert.Throws<InvalidOperationException>(() => a.Remove(1));
        a.Unequip(EquipmentSlot.Weapon);
        a.Remove(1);
        b.Add(sword);
        new ItemUpgradeService(b).Upgrade(1);
        Assert.Empty(a.Items);
        Assert.Equal(1, a.Attack);
    }
}
