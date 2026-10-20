namespace Backpack;

public abstract class InventoryItem
{
    public int Id { get; }
    public string Name { get; }
    internal PlayerInventory? Container { get; set; }

    protected InventoryItem(int id, string name)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Название обязательно.");
        Id = id;
        Name = name.Trim();
    }

    public abstract string Describe();
}
