namespace Backpack;

public interface IItemLookup
{
    InventoryItem GetItem(int itemId);
}

public sealed class PlayerInventory : IItemLookup
{
    private readonly Dictionary<int, InventoryItem> items = [];
    private readonly Dictionary<EquipmentSlot, InventoryItem> equipped = [];
    public PlayerStats Player { get; }
    public int Attack => checked(Player.BaseAttack + equipped.Values.Cast<IEquipment>().Sum(e => e.AttackBonus));
    public int Defense => checked(Player.BaseDefense + equipped.Values.Cast<IEquipment>().Sum(e => e.DefenseBonus));
    public IReadOnlyList<InventoryItem> Items => items.Values.OrderBy(i => i.Id).ToArray();

    public PlayerInventory(PlayerStats player)
    {
        ArgumentNullException.ThrowIfNull(player);
        Player = player;
    }

    public void Add(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item is IConsumable { IsUsed: true })
            throw new InvalidOperationException("Израсходованный предмет нельзя добавить.");
        if (item.Container is not null)
            throw new InvalidOperationException("Предмет уже принадлежит инвентарю.");
        if (!items.TryAdd(item.Id, item)) throw new ArgumentException("Этот номер предмета уже занят.");
        item.Container = this;
    }

    public InventoryItem GetItem(int itemId) => items.TryGetValue(itemId, out var item)
        ? item : throw new KeyNotFoundException("Предмет не найден.");

    public void Remove(int itemId)
    {
        InventoryItem item = GetItem(itemId);
        if (equipped.Values.Contains(item))
            throw new InvalidOperationException("Сначала снимите экипировку.");
        items.Remove(itemId);
        item.Container = null;
    }

    public void Equip(int itemId)
    {
        InventoryItem item = GetItem(itemId);
        if (item is not IEquipment equipment)
            throw new InvalidOperationException("Предмет нельзя экипировать.");
        equipped[equipment.Slot] = item;
    }

    public bool Unequip(EquipmentSlot slot) => equipped.Remove(slot);
    public bool IsEquipped(int itemId) => equipped.Values.Any(item => item.Id == itemId);

    public void Use(int itemId)
    {
        InventoryItem item = GetItem(itemId);
        if (item is not IConsumable consumable)
            throw new InvalidOperationException("Предмет нельзя употребить.");
        consumable.Use(Player);
        if (consumable.IsUsed && ReferenceEquals(item.Container, this))
            Remove(itemId);
    }

    public IReadOnlyList<string> DescribeContents() => Items.Select(item =>
        item.Describe() + (IsEquipped(item.Id) ? " [экипирован]" : "")).ToArray();
}
