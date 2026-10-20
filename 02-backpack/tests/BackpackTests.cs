using Backpack;
using Xunit;

namespace Backpack.Checks;

public class BackpackTests
{
    [Fact]
    public void DifferentItemsKeepTheirUniqueProperties()
    {
        var inventory = CreateInventory();
        Assert.Equal(8, Assert.IsType<Sword>(inventory.GetItem(1)).AttackBonus);
        Assert.Equal(5, Assert.IsType<BodyArmor>(inventory.GetItem(2)).DefenseBonus);
        Assert.Equal(30, Assert.IsType<HealthPotion>(inventory.GetItem(3)).Healing);
        Assert.Equal("Старый маяк", Assert.IsType<QuestObject>(inventory.GetItem(4)).Quest);
        Assert.Equal(4, inventory.DescribeContents().Count);
    }

    [Fact]
    public void EquipmentChangesCombatStatsAndCanBeRemoved()
    {
        var inventory = CreateInventory();
        inventory.Equip(1);
        inventory.Equip(2);
        Assert.Equal(10, inventory.Attack);
        Assert.Equal(6, inventory.Defense);
        Assert.Contains("[экипирован]", inventory.DescribeContents()[0]);
        Assert.True(inventory.Unequip(EquipmentSlot.Weapon));
        Assert.False(inventory.Unequip(EquipmentSlot.Weapon));
        Assert.Equal(2, inventory.Attack);
        Assert.Equal(6, inventory.Defense);
    }

    [Fact]
    public void ReplacingWeaponKeepsOldWeaponWithoutStackingBonuses()
    {
        var inventory = CreateInventory();
        inventory.Add(new Sword(5, "Стальной", 12));
        inventory.Equip(1);
        inventory.Equip(5);
        Assert.Equal(14, inventory.Attack);
        Assert.False(inventory.IsEquipped(1));
        Assert.True(inventory.IsEquipped(5));
        Assert.Equal(5, inventory.Items.Count);
        inventory.Equip(5);
        Assert.Equal(14, inventory.Attack);
    }

    [Fact]
    public void PotionHealsAndIsConsumed()
    {
        var inventory = CreateInventory();
        inventory.Player.TakeDamage(40);
        inventory.Use(3);
        Assert.Equal(90, inventory.Player.Health);
        Assert.Throws<KeyNotFoundException>(() => inventory.GetItem(3));
    }

    [Fact]
    public void PotionDoesNotExceedMaximumHealth()
    {
        var inventory = CreateInventory();
        inventory.Player.TakeDamage(5);
        inventory.Use(3);
        Assert.Equal(100, inventory.Player.Health);
    }

    [Fact]
    public void PotionAtFullHealthIsPreserved()
    {
        var inventory = CreateInventory();
        Assert.Throws<InvalidOperationException>(() => inventory.Use(3));
        Assert.IsType<HealthPotion>(inventory.GetItem(3));
        Assert.Equal(100, inventory.Player.Health);
    }

    [Fact]
    public void UnsupportedActionsDoNotRemoveQuestObject()
    {
        var inventory = CreateInventory();
        Assert.Throws<InvalidOperationException>(() => inventory.Use(4));
        Assert.Throws<InvalidOperationException>(() => inventory.Equip(4));
        Assert.Throws<InvalidOperationException>(() => new ItemUpgradeService(inventory).Upgrade(4));
        Assert.IsType<QuestObject>(inventory.GetItem(4));
    }

    [Fact]
    public void UpgradeUsesDifferentGrowthForSwordAndBodyArmor()
    {
        var inventory = CreateInventory();
        var service = new ItemUpgradeService(inventory);
        service.Upgrade(1);
        service.Upgrade(2);
        Assert.Equal(11, Assert.IsType<Sword>(inventory.GetItem(1)).AttackBonus);
        Assert.Equal(7, Assert.IsType<BodyArmor>(inventory.GetItem(2)).DefenseBonus);
    }

    [Fact]
    public void UpgradingEquippedGearImmediatelyChangesStats()
    {
        var inventory = CreateInventory();
        inventory.Equip(1);
        new ItemUpgradeService(inventory).Upgrade(1);
        Assert.Equal(13, inventory.Attack);
    }

    [Fact]
    public void UpgradeLimitPreservesMaximumLevel()
    {
        var inventory = CreateInventory();
        var service = new ItemUpgradeService(inventory);
        for (int i = 0; i < Gear.MaxLevel; i++) service.Upgrade(1);
        Assert.Throws<InvalidOperationException>(() => service.Upgrade(1));
        Assert.Equal(3, Assert.IsType<Sword>(inventory.GetItem(1)).Level);
        Assert.Equal(17, Assert.IsType<Sword>(inventory.GetItem(1)).AttackBonus);
    }

    [Fact]
    public void UpgradeServiceCanWorkWithoutConcreteInventory()
    {
        var sword = new Sword(9, "Учебный", 4);
        var service = new ItemUpgradeService(new OneItemLookup(sword));
        service.Upgrade(9);
        Assert.Equal(7, sword.AttackBonus);
    }

    [Fact]
    public void EquippedItemMustBeUnequippedBeforeRemoval()
    {
        var inventory = CreateInventory();
        inventory.Equip(2);
        Assert.Throws<InvalidOperationException>(() => inventory.Remove(2));
        inventory.Unequip(EquipmentSlot.Protection);
        inventory.Remove(2);
        Assert.Throws<KeyNotFoundException>(() => inventory.GetItem(2));
    }

    [Fact]
    public void DuplicateAndUnknownItemIdsAreRejected()
    {
        var inventory = CreateInventory();
        Assert.Throws<ArgumentException>(() => inventory.Add(new Sword(1, "Другой", 2)));
        Assert.Throws<KeyNotFoundException>(() => inventory.Equip(99));
        Assert.Throws<KeyNotFoundException>(() => inventory.Use(99));
        Assert.Throws<KeyNotFoundException>(() => new ItemUpgradeService(inventory).Upgrade(99));
        Assert.Equal(4, inventory.Items.Count);
    }

    [Fact]
    public void InvalidValuesAndExcessDamageAreHandled()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Sword(1, "Меч", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new HealthPotion(1, "Зелье", -1));
        var player = new PlayerStats(100);
        Assert.Throws<ArgumentOutOfRangeException>(() => player.TakeDamage(-1));
        player.TakeDamage(1000);
        Assert.Equal(0, player.Health);
    }

    private static PlayerInventory CreateInventory()
    {
        var inventory = new PlayerInventory(new PlayerStats(100, 2, 1));
        inventory.Add(new Sword(1, "Бронзовый", 8));
        inventory.Add(new BodyArmor(2, "Кожаный", 5));
        inventory.Add(new HealthPotion(3, "Малое", 30));
        inventory.Add(new QuestObject(4, "Ключ смотрителя", "Старый маяк"));
        return inventory;
    }

    private sealed class OneItemLookup : IItemLookup
    {
        private readonly InventoryItem item;
        public OneItemLookup(InventoryItem item) => this.item = item;
        public InventoryItem GetItem(int itemId) => item.Id == itemId
            ? item : throw new KeyNotFoundException();
    }
}
