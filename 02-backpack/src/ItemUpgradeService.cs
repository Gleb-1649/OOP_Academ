namespace Backpack;

public sealed class ItemUpgradeService
{
    private readonly IItemLookup inventory;

    public ItemUpgradeService(IItemLookup inventory)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        this.inventory = inventory;
    }

    public void Upgrade(int itemId)
    {
        InventoryItem item = inventory.GetItem(itemId);
        if (item is not IUpgradeable upgradable)
            throw new InvalidOperationException("Этот предмет нельзя улучшить.");
        upgradable.Improve();
    }
}
